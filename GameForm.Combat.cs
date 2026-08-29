using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Font = System.Drawing.Font;

namespace Rogalik
{
    public partial class GameForm
    {
        private float multicastChance = 0f;
        private readonly Dictionary<string, float> abilityDmgMult = new();
        private int killCount = 0;

        private float GetAbilityDamageMult(string id)
        {
            if (abilityDmgMult.TryGetValue(id, out var m) && m > 0f) return m;
            return 1f;
        }

        partial void RefreshStatsPanel();

        private void DealSkillDamage(Enemy en, int dmg)
        {
            dmg = Math.Max(1, dmg);
            en.HP -= dmg;
            if (en.HP < 0) en.HP = 0;
            SpawnDamageNumber(en, dmg, false);
        }

        private void ApplyDamageToPlayer(int dmg)
        {
            int final = Math.Max(1, dmg - (int)Math.Round(player.Armor));
            player.HP -= final;
            if (player.HP < 0) player.HP = 0;
            UpdateHpUi();
            if (player.HP <= 0) OnPlayerDeath();
        }

        private void UpdateHpUi()
        {
            hpBar.Maximum = player.MaxHP;
            hpBar.Value = Math.Max(hpBar.Minimum, Math.Min(player.HP, player.MaxHP));
        }

        private void UpdateEffects(int dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var ef = effects[i];
                ef.Update(this, dt);
                if (ef.Dead) effects.RemoveAt(i);
            }
        }

        private abstract class EffectBase
        {
            public int LifeMs;
            public bool Dead;
            public virtual void Update(GameForm g, int dt)
            {
                LifeMs -= dt;
                if (LifeMs <= 0) Dead = true;
            }
            public abstract void Draw(Graphics g);
        }

        private class DamageNumber : EffectBase
        {
            float x, y;
            readonly int life0;
            readonly string text;
            readonly bool crit;
            public DamageNumber(float x, float y, int dmg, bool crit, int lifeMs = 650)
            {
                this.x = x; this.y = y;
                this.text = Math.Min(Math.Max(0, dmg), 999).ToString();
                this.crit = crit;
                LifeMs = lifeMs;
                life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                y -= 0.04f * dt;
                LifeMs -= dt;
                if (LifeMs <= 0) Dead = true;
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                using var f = new Font("Segoe UI", crit ? 12f : 11f, crit ? FontStyle.Bold : FontStyle.Regular);
                var col = crit ? Color.FromArgb((int)(255 * a), 255, 230, 120)
                               : Color.FromArgb((int)(255 * a), 255, 220, 220);
                var shadow = Color.FromArgb((int)(180 * a), 0, 0, 0);
                var size = g.MeasureString(text, f);
                var rect = new RectangleF(x - size.Width / 2f, y - size.Height / 2f, size.Width, size.Height);
                using var brShadow = new SolidBrush(shadow);
                g.DrawString(text, f, brShadow, rect.X + 1, rect.Y + 1);
                using var br = new SolidBrush(col);
                g.DrawString(text, f, br, rect);
            }
        }

        private void SpawnDamageNumber(Enemy en, int dmg, bool crit)
        {
            effects.Add(new DamageNumber(en.X + en.W / 2f, en.Y - 8, dmg, crit));
        }

        private class Particle : EffectBase
        {
            float x, y, vx, vy, size;
            Color c;
            int life0;
            public Particle(float x, float y, float vx, float vy, float size, int lifeMs, Color c)
            {
                this.x = x; this.y = y; this.vx = vx; this.vy = vy; this.size = size;
                this.c = c; LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                x += vx * dt / 16f;
                y += vy * dt / 16f;
                vy += 0.0008f * dt;
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                float s = size * (0.7f + 0.3f * a);
                using var gp = new GraphicsPath();
                gp.AddEllipse(x - s * 1.8f, y - s * 1.8f, s * 3.6f, s * 3.6f);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb((int)(140 * a), c),
                    SurroundColors = new[] { Color.FromArgb(0, c) }
                };
                g.FillPath(glow, gp);
                using var br = new SolidBrush(Color.FromArgb((int)(220 * a), c));
                g.FillEllipse(br, x - s, y - s, s * 2, s * 2);
            }
        }

        private class DelayedAction : EffectBase
        {
            private int left;
            private readonly Action action;
            private bool done;
            public DelayedAction(int delayMs, Action act)
            {
                LifeMs = delayMs + 10;
                left = delayMs;
                action = act;
            }
            public override void Update(GameForm g, int dt)
            {
                left -= dt;
                if (!done && left <= 0)
                {
                    done = true;
                    action?.Invoke();
                }
                base.Update(g, dt);
            }
            public override void Draw(Graphics g) { }
        }

        private class BuffTimer : EffectBase
        {
            private readonly Action onStart, onEnd;
            private bool started = false;
            public BuffTimer(int durationMs, Action onStart, Action onEnd)
            {
                LifeMs = durationMs;
                this.onStart = onStart;
                this.onEnd = onEnd;
            }
            public override void Update(GameForm g, int dt)
            {
                if (!started)
                {
                    started = true;
                    onStart?.Invoke();
                }
                base.Update(g, dt);
                if (Dead) onEnd?.Invoke();
            }
            public override void Draw(Graphics g) { }
        }

        private class RingPulse : EffectBase
        {
            float x, y, r;
            int life0;
            Color c1, c2;
            Action<Enemy> hit;
            bool dealt = false;
            public RingPulse(float x, float y, float radius, int lifeMs, Color c1, Color c2, Action<Enemy> onHit)
            {
                this.x = x; this.y = y; this.r = radius; this.c1 = c1; this.c2 = c2;
                hit = onHit; LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        double d2 = (x - ex) * (x - ex) + (y - ey) * (y - ey);
                        if (d2 <= r * r) hit?.Invoke(e);
                    }
                    dealt = true; hit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                float w = 8 + 10 * a;
                using var pen = new Pen(Color.FromArgb((int)(180 * a), c1), w);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);

                using var gp = new GraphicsPath();
                gp.AddEllipse(x - r * 1.1f, y - r * 1.1f, r * 2.2f, r * 2.2f);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb((int)(80 * a), c2),
                    SurroundColors = new[] { Color.FromArgb(0, c2) }
                };
                g.FillPath(glow, gp);
            }
        }

        private class RingSlash : EffectBase
        {
            float x, y, r; int life0; Color c; Action<Enemy> onHit; bool dealt;
            public RingSlash(float x, float y, float r, int lifeMs, Color c, Action<Enemy> onHit)
            {
                this.x = x; this.y = y; this.r = r; this.c = c; this.onHit = onHit;
                LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        double d2 = (x - ex) * (x - ex) + (y - ey) * (y - ey);
                        if (d2 <= r * r) onHit?.Invoke(e);
                    }
                    dealt = true; onHit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                using var pen = new Pen(Color.FromArgb((int)(200 * a), c), 10);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            }
        }

        private class ArcSlash : EffectBase
        {
            Player p; float arcDeg; float radius; int life0; Color c; Action<Enemy> onHit; bool dealt;
            public ArcSlash(Player p, float arcDeg, float radius, int lifeMs, Color c, Action<Enemy> onHit)
            {
                this.p = p; this.arcDeg = arcDeg; this.radius = radius; this.c = c; this.onHit = onHit;
                LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                    float ang = FacingToAngle(p.Facing);
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (InsideCone(cx, cy, ang, arcDeg, radius, ex, ey)) onHit?.Invoke(e);
                    }
                    dealt = true; onHit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                float start = FacingToAngle(p.Facing) - arcDeg / 2f;
                using var path = new GraphicsPath();
                path.AddPie(cx - radius, cy - radius, radius * 2, radius * 2, start, arcDeg);
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                using var br = new SolidBrush(Color.FromArgb((int)(160 * a), c));
                using var pen = new Pen(Color.FromArgb((int)(220 * a), c), 3);
                g.FillPath(br, path);
                g.DrawPath(pen, path);
            }
        }

        private class ShieldBash : EffectBase
        {
            Player p; float arcDeg, radius; int life0; Color c; int knock; Action<Enemy> onHit; bool dealt;
            public ShieldBash(Player p, float arcDeg, float radius, int lifeMs, Color c, int knockback, Action<Enemy> onHit)
            {
                this.p = p; this.arcDeg = arcDeg; this.radius = radius; this.c = c; this.knock = knockback;
                this.onHit = onHit; LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                    float ang = FacingToAngle(p.Facing);
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (InsideCone(cx, cy, ang, arcDeg, radius, ex, ey))
                        {
                            onHit?.Invoke(e);
                            float dx = ex - cx, dy = ey - cy;
                            float len = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (len > 0.001f) { dx /= len; dy /= len; }
                            e.X += dx * knock;
                            e.Y += dy * knock;
                        }
                    }
                    dealt = true; onHit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                float start = FacingToAngle(p.Facing) - arcDeg / 2f;
                using var path = new GraphicsPath();
                path.AddPie(cx - radius, cy - radius, radius * 2, radius * 2, start, arcDeg);
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                using var br = new SolidBrush(Color.FromArgb((int)(140 * a), c));
                g.FillPath(br, path);
            }
        }

        private class ConePulse : EffectBase
        {
            Player p; float arc, radius; int tickMs, accum; Color c; Action<Enemy> onTick;
            public ConePulse(Player p, float arcDeg, float radius, int durationMs, int tickMs, Color c, Action<Enemy> onTick)
            {
                this.p = p; this.arc = arcDeg; this.radius = radius; this.tickMs = tickMs; this.c = c; this.onTick = onTick;
                LifeMs = durationMs; accum = 0;
            }

            public override void Update(GameForm G, int dt)
            {
                accum += dt;
                while (accum >= tickMs)
                {
                    accum -= tickMs;
                    float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                    float ang = FacingToAngle(p.Facing);
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (InsideCone(cx, cy, ang, arc, radius, ex, ey)) onTick?.Invoke(e);
                    }
                }
                base.Update(G, dt);
            }

            public override void Draw(Graphics g)
            {
                float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                float start = FacingToAngle(p.Facing) - arc / 2f;
                using var path = new GraphicsPath();
                path.AddPie(cx - radius, cy - radius, radius * 2, radius * 2, start, arc);
                using var br = new SolidBrush(Color.FromArgb(90, 255, 215, 0));
                g.FillPath(br, path);
            }
        }

        private class GroundZone : EffectBase
        {
            float x, y, r; int tick, accum; Color c; Action<Enemy> onTick; Action onEnd;
            public GroundZone(float x, float y, float r, int duration, int tickMs, Color c, Action<Enemy> onTick, Action onEnd)
            {
                this.x = x; this.y = y; this.r = r; this.c = c;
                this.onTick = onTick; this.onEnd = onEnd;
                LifeMs = duration; tick = tickMs;
            }

            public override void Update(GameForm G, int dt)
            {
                accum += dt;
                while (accum >= tick)
                {
                    accum -= tick;
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (Dist2(x, y, ex, ey) <= r * r) onTick?.Invoke(e);
                    }
                }
                base.Update(G, dt);
                if (Dead) onEnd?.Invoke();
            }
            public override void Draw(Graphics g)
            {
                using var gp = new GraphicsPath();
                gp.AddEllipse(x - r, y - r, r * 2, r * 2);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb(90, 255, 225, 120),
                    SurroundColors = new[] { Color.FromArgb(0, 255, 225, 120) }
                };
                g.FillPath(glow, gp);
                using var pen = new Pen(Color.FromArgb(160, 255, 215, 0), 2);
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            }
        }

        private class Explosion : EffectBase
        {
            float x, y, r; int life0; Color c; Action<Enemy> onHit; bool dealt = false;
            public Explosion(float x, float y, float r, int lifeMs, Color c, Action<Enemy> onHit)
            {
                this.x = x; this.y = y; this.r = r; this.c = c; this.onHit = onHit;
                LifeMs = lifeMs; life0 = lifeMs;
            }

            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (Dist2(x, y, ex, ey) <= r * r) onHit?.Invoke(e);
                    }
                    dealt = true; onHit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                float rr = r * (0.9f + 0.1f * a);

                using var gp = new GraphicsPath();
                gp.AddEllipse(x - rr, y - rr, rr * 2, rr * 2);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb((int)(180 * a), 255, 215, 0),
                    SurroundColors = new[] { Color.FromArgb(0, 255, 215, 0) }
                };
                g.FillPath(glow, gp);

                using var ring = new Pen(Color.FromArgb((int)(220 * a), 255, 235, 120), 4);
                g.DrawEllipse(ring, x - rr, y - rr, rr * 2, rr * 2);
            }
        }

        private class VoidKill : EffectBase
        {
            float x, y, r; Color c; Action<Enemy> kill; bool dealt = false;
            public VoidKill(float x, float y, float r, int lifeMs, Color c, Action<Enemy> kill)
            {
                this.x = x; this.y = y; this.r = r; this.c = c; this.kill = kill;
                LifeMs = lifeMs;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (Dist2(x, y, ex, ey) <= r * r) kill?.Invoke(e);
                    }
                    dealt = true; kill = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                using var gp = new GraphicsPath();
                gp.AddEllipse(x - r, y - r, r * 2, r * 2);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb(120, 255, 215, 0),
                    SurroundColors = new[] { Color.FromArgb(0, 255, 215, 0) }
                };
                g.FillPath(glow, gp);
            }
        }

        private class BeamStrike : EffectBase
        {
            float x; float y1, y2; float hitRadius; Color c; Action<Enemy> onHit; bool dealt = false;
            public BeamStrike(float x, float y1, float y2, int lifeMs, Color c, Action<Enemy> onHit)
            {
                this.x = x; this.y1 = y1; this.y2 = y2; this.c = c; this.onHit = onHit;
                LifeMs = lifeMs; hitRadius = 40;
            }
            public override void Update(GameForm G, int dt)
            {
                if (!dealt)
                {
                    float cy = (y1 + y2) / 2f;
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (Math.Abs(ex - x) <= 30 && Math.Abs(ey - cy) <= hitRadius) onHit?.Invoke(e);
                    }
                    dealt = true; onHit = null;
                }
                base.Update(G, dt);
            }
            public override void Draw(Graphics g)
            {
                using var penCore = new Pen(Color.FromArgb(230, 255, 230, 140), 6);
                g.DrawLine(penCore, x, y1, x, y2);

                using var penOuter = new Pen(Color.FromArgb(100, 255, 215, 0), 16);
                g.DrawLine(penOuter, x, y1, x, y2);
            }
        }

        private class ChainLine : EffectBase
        {
            float x1, y1, x2, y2; int life0;
            public ChainLine(float x1, float y1, float x2, float y2, int lifeMs)
            {
                this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2;
                LifeMs = lifeMs; life0 = lifeMs;
            }
            public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));
                using var pen = new Pen(Color.FromArgb((int)(220 * a), 255, 235, 120), 3);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void GetAimDirToNearest(out float vx, out float vy)
        {
            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;
            var t = NearestEnemy(px, py);
            if (t != null)
            {
                float tx = t.X + t.W / 2f, ty = t.Y + t.H / 2f;
                float dx = tx - px, dy = ty - py;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len > 0.0001f) { vx = dx / len; vy = dy / len; return; }
            }
            vx = (player.Facing == Facing.Left ? -1 : player.Facing == Facing.Right ? 1 : 0);
            vy = (player.Facing == Facing.Up ? -1 : player.Facing == Facing.Down ? 1 : 0);
            if (vx == 0 && vy == 0) vy = 1;
        }

        private void FireForwardBullet(int dmg, int pierce, int chain, float speed, int radius = 4)
        {
            float dirx = player.Facing == Facing.Left ? -1 : player.Facing == Facing.Right ? 1 : 0;
            float diry = player.Facing == Facing.Up ? -1 : player.Facing == Facing.Down ? 1 : 0;
            if (dirx == 0 && diry == 0) diry = 1;
            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;
            bullets.Add(new Bullet
            {
                X = px,
                Y = py,
                VX = dirx,
                VY = diry,
                Speed = speed,
                Damage = dmg,
                LifeMs = 1200,
                Radius = radius,
                Pierce = pierce,
                Chain = chain
            });
        }

        private void FireTargetedBullet(int dmg, int pierce, int chain, float speed, int radius = 4)
        {
            GetAimDirToNearest(out float vx, out float vy);
            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;
            bullets.Add(new Bullet
            {
                X = px,
                Y = py,
                VX = vx,
                VY = vy,
                Speed = speed,
                Damage = dmg,
                LifeMs = 1200,
                Radius = radius,
                Pierce = pierce,
                Chain = chain
            });
        }

        private void ChainDamage(float sx, float sy, Enemy first, int jumps, int dmg)
        {
            var visited = new HashSet<Enemy>();
            Enemy cur = first;
            float prevx = sx, prevy = sy;

            for (int i = 0; i < 1 + jumps && cur != null; i++)
            {
                DealSkillDamage(cur, dmg);
                effects.Add(new ChainLine(prevx, prevy, cur.X + cur.W / 2f, cur.Y + cur.H / 2f, 220));
                prevx = cur.X + cur.W / 2f;
                prevy = cur.Y + cur.H / 2f;
                visited.Add(cur);
                cur = NearestEnemy(prevx, prevy, e => !visited.Contains(e));
            }
        }

        private int GetXpToNext(int currentLevel)
        {
            if (currentLevel <= 2) return 45;
            if (currentLevel == 3) return 60;
            return 60 + (currentLevel - 2) * 18;
        }

        private void GainXP(int amount)
        {
            xp += amount;
            while (xp >= xpToNext)
            {
                xp -= xpToNext;
                LevelUp();

                xpToNext = GetXpToNext(level);
            }
        }

        private void OnPlayerDeath()
        {
            enemySpawnTimer.Stop();
            gameTimer.Stop();
            ShowCursor();
            ResetMovementKeys();
            overlayGameOver.Visible = true;
            CenterGameOverBox();
            overlayGameOver.BringToFront();
            Save.HighScore = Math.Max(Save.HighScore, killCount);
        }

        private void RestartGame()
        {
            overlayGameOver.Visible = false;
            LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
            enemies.Clear();
            bullets.Clear();
            effects.Clear();
            abilityDmgMult.Clear();
            buffLevels.Clear();
            levelUpStep = 0;
            killCount = 0;
            CreatePlayer();
            RefreshStatsPanel();
            BeginIntroDialogue();
        }

        private void LevelUp()
        {
            level++;

            player.HP = player.MaxHP;
            UpdateHpUi();

            PauseGame();
            ShowCursor();

            bool pickAbility = (levelUpStep == 0);
            var rawChoices = pickAbility ? GenerateAbilityOptions(3) : GenerateBuffOptions(3);
            var choices = PrepareCardsForPicker(rawChoices);

            if (choices.Count == 0)
            {
                HideCursorIfFullscreen();
                ResumeGame();
                return;
            }

            picker.ShowChoices(choices, picked =>
            {
                if (picked.Id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase))
                    ApplyBuff(picked.Id);
                else
                    AddOrUpgradeAbility(picked);

                levelUpStep = (levelUpStep + 1) % 4;

                picker.HideOverlay();
                HideCursorIfFullscreen();
                Focus();
                Select();
                RefreshStatsPanel();
                ResumeGame();
            });
        }

        private List<AbilityDef> GenerateBuffOptions(int count)
        {
            var all = BuffCatalog.All.ToList();

            bool critCapped = player.CritChance >= 0.999f;
            bool multiCapped = multicastChance >= 0.999f;

            all = all.Where(b =>
            {
                if (b.Id == "bf_crit_chance" && critCapped) return false;
                if (b.Id == "bf_multicast" && multiCapped) return false;
                return true;
            }).ToList();

            if (all.Count == 0) all = BuffCatalog.All.ToList();

            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }

            return all.Take(Math.Max(0, Math.Min(count, all.Count)))
                      .Select(BuffCatalog.ToAbilityCard)
                      .ToList();
        }

        private List<AbilityDef> GenerateAbilityOptions(int count)
        {
            var banned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var owned = abilityLevels.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newPool = AbilityCatalog.All
                .Where(a => !owned.Contains(a.Id) && !banned.Contains(a.Id))
                .ToList();

            var upgPool = AbilityCatalog.All
                .Where(a => owned.Contains(a.Id) && abilityLevels[a.Id] < MaxStackLevel && !banned.Contains(a.Id))
                .ToList();

            var res = new List<AbilityDef>();
            void Shuffle<T>(IList<T> list)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }

            if (owned.Count >= MaxUniqueAbilities)
            {
                Shuffle(upgPool);
                res.AddRange(upgPool.Take(count));
            }
            else
            {
                Shuffle(newPool);
                res.AddRange(newPool.Take(count));
                if (res.Count < count)
                {
                    Shuffle(upgPool);
                    res.AddRange(upgPool.Take(count - res.Count));
                }
            }
            return res;
        }

        private void AddOrUpgradeAbility(AbilityDef def)
        {
            abilityLevels.TryGetValue(def.Id, out var cur);
            bool isNew = cur == 0;
            if (isNew && abilityLevels.Count >= MaxUniqueAbilities) return;

            int next = Math.Min(MaxStackLevel, cur + 1);
            abilityLevels[def.Id] = next;
            if (isNew) abilityOrder.Add(def.Id);

            if (!isNew && abilityOrder.Count >= MaxUniqueAbilities)
            {
                float now = GetAbilityDamageMult(def.Id);
                abilityDmgMult[def.Id] = now * 1.25f;
            }

            ApplyAbilityEffect(def.Id, next);

            if (!IsPermanentBuff(def.Id))
                if (!cdLeftMs.ContainsKey(def.Id))
                    cdLeftMs[def.Id] = 0;

            UpdateHpUi();
        }

        private void ApplyBuff(string id)
        {
            buffLevels.TryGetValue(id, out int cur);
            int next = cur + 1;
            buffLevels[id] = next;

            switch (id)
            {
                case "bf_hp":
                    {
                        player.MaxHP = (int)Math.Round(player.MaxHP * 1.30);
                        player.HP = player.MaxHP;
                        UpdateHpUi();
                        break;
                    }

                case "bf_speed":
                    {
                        player.SpeedMult *= 1.10f;
                        break;
                    }

                case "bf_xp":
                    {
                        player.XpGainMult *= 1.25f;
                        break;
                    }

                case "bf_armor":
                    {
                        player.Armor += 2f;
                        break;
                    }

                case "bf_crit_chance":
                    {
                        player.CritChance += 0.05f;
                        if (player.CritChance > 1f) player.CritChance = 1f;
                        break;
                    }

                case "bf_crit_mult":
                    {
                        player.CritMult += 0.15f;
                        break;
                    }

                case "bf_multicast":
                    {
                        multicastChance += 0.10f;
                        if (multicastChance > 1f) multicastChance = 1f;
                        break;
                    }
            }
    }

        private void ApplyAbilityEffect(string id, int newLevel)
        {
            float baseScale = 1f + 0.25f * Math.Max(0, newLevel - 1);
            abilityDmgMult[id] = baseScale;
        }

        private bool IsPermanentBuff(string id)
            => !string.IsNullOrWhiteSpace(id) && id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase);

        private void UpdateActiveAbilities(int dt)
        {
            var keys = cdLeftMs.Keys.ToList();
            foreach (var id in keys)
            {
                cdLeftMs[id] -= dt;
                if (cdLeftMs[id] <= 0)
                {
                    CastAbility(id);

                    if (multicastChance > 0f && rng.NextDouble() < multicastChance)
                        CastAbility(id);

                    cdLeftMs[id] = GetCooldownMs(id);
                }
            }
        }

        private void PlayerAutoShoot()
        {
            if (enemies.Count == 0) return;

            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;
            Enemy target = NearestEnemy(px, py);
            if (target == null) return;

            float tx = target.X + target.W / 2f, ty = target.Y + target.H / 2f;
            float dx = tx - px, dy = ty - py;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len <= 0.0001f) return;
            dx /= len; dy /= len;

            int count = Math.Max(1, player.ProjectileCount);
            float spreadDeg = Math.Min(36f, 8f * (count - 1));
            float step = count > 1 ? spreadDeg / (count - 1) : 0f;
            float start = -spreadDeg / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = (float)(Math.Atan2(dy, dx) + (Math.PI / 180.0) * (start + step * i));
                float vx = (float)Math.Cos(angle), vy = (float)Math.Sin(angle);

                bullets.Add(new Bullet
                {
                    X = px,
                    Y = py,
                    VX = vx,
                    VY = vy,
                    Speed = player.ProjectileSpeed,
                    Damage = player.Damage,
                    LifeMs = 1200,
                    Radius = 4,
                    Pierce = 0,
                    Chain = 0
                });
            }
        }

        private AbilityDef CloneAbilityDef(AbilityDef src)
        {
            return new AbilityDef
            {
                Id = src.Id,
                Name = src.Name,
                Kind = src.Kind,
                Types = src.Types,
                ShortText = src.ShortText,
                Icon = src.Icon,
                ShowOrderBadge = src.ShowOrderBadge,
                Stats = src.Stats?.Select(s => new StatLine(s.Label, s.Value, s.ValueColor)).ToList()
                        ?? new List<StatLine>()
            };
        }

        private static int ExtractFirstInt(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int val = 0; bool ok = false;
            int cur = 0; bool inNum = false;
            foreach (var ch in s)
            {
                if (char.IsDigit(ch))
                {
                    inNum = true;
                    cur = cur * 10 + (ch - '0');
                    ok = true;
                }
                else if (inNum) break;
            }
            if (ok) val = cur;
            return val;
        }

        private List<AbilityDef> PrepareCardsForPicker(List<AbilityDef> raw)
        {
            var list = new List<AbilityDef>(raw.Count);
            foreach (var ab in raw)
            {
                if (ab?.Id == null) { list.Add(ab); continue; }

                bool isBuff = ab.Id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase);
                if (isBuff)
                {
                    list.Add(CloneAbilityDef(ab));
                    continue;
                }

                abilityLevels.TryGetValue(ab.Id, out int curLv);
                int nextLv = curLv + 1;

                if (curLv <= 0)
                {
                    list.Add(CloneAbilityDef(ab));
                    continue;
                }

                float curMult = 1f + 0.25f * Math.Max(0, curLv - 1);
                float nextMult = 1f + 0.25f * Math.Max(0, nextLv - 1);

                var copy = CloneAbilityDef(ab);
                copy.Types = string.IsNullOrWhiteSpace(ab.Types) ? "Усиление" : (ab.Types + ", Усиление");
                copy.ShortText = (ab.ShortText ?? "") + $"  •  Усиление до ур. {nextLv}";

                foreach (var st in copy.Stats)
                {
                    string lbl = st.Label?.Trim().ToLowerInvariant();
                    if (lbl == null) continue;

                    bool isDmg = lbl.Contains("урон") || lbl.Contains("частота") || lbl.Contains("взрыв урон");
                    if (!isDmg) continue;

                    int baseVal = ExtractFirstInt(st.Value);
                    if (baseVal <= 0) continue;

                    int curVal = (int)Math.Round(baseVal * curMult);
                    int nxtVal = (int)Math.Round(baseVal * nextMult);
                    int delta = Math.Max(0, nxtVal - curVal);

                    st.Value = $"{nxtVal} (+{delta})";
                    st.ValueColor = Color.FromArgb(0, 220, 100);
                }

                list.Add(copy);
            }
            return list;
        }

    }
}