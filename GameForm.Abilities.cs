using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Rogalik
{
    public partial class GameForm
    {
        private int GetCooldownMs(string id) => id switch
        {
            "ab1" => 10000,
            "ab2" => 12000,
            "ab3" => 3000,
            "ab4" => 5000,
            "ab5" => 6000,
            "ab6" => 4000,
            "ab7" => 5000,
            "ab8" => 6000,
            "ab9" => 8000,
            "ab10" => 5000,
            "ab11" => 10000,
            "ab12" => 20000,
            "ab13" => 30000,
            "ab14" => 20000,
            "ab15" => 10000,
            "ab16" => 8000,
            "ab17" => 5000,
            "ab18" => 20000,
            "ab19" => 3000,
            "ab20" => 10000,
            "ab21" => 10000,
            "ab22" => 14000,
            "ab23" => 25000,
            "ab24" => 8000,
            "ab25" => 5000,
            "ab26" => 20000,
            "ab27" => 6000,
            "ab28" => 3000,
            "ab29" => 4000,
            "ab30" => 12000,
            _ => 10000
        };

        private void CastAbility(string id)
        {
            Color y1 = Color.FromArgb(210, 255, 215, 0);
            Color y2 = Color.FromArgb(200, 255, 235, 140);
            Color hot = Color.FromArgb(190, 255, 160, 60);
            Color white = Color.FromArgb(200, 255, 255, 240);

            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;

            void Burst(float sx, float sy, int count, float speedMin, float speedMax, Color c)
            {
                for (int i = 0; i < count; i++)
                {
                    float ang = (float)(rng.NextDouble() * Math.PI * 2);
                    float sp = (float)(speedMin + rng.NextDouble() * (speedMax - speedMin));
                    float vx = (float)Math.Cos(ang) * sp;
                    float vy = (float)Math.Sin(ang) * sp;
                    effects.Add(new Particle(sx, sy, vx, vy, rng.Next(2, 5), rng.Next(350, 650), c));
                }
            }

            void TelegraphCircle(float x, float y, float r, int prepMs, Color c)
            {
                effects.Add(new RingPulse(x, y, r, prepMs, c, white, null));
            }

            void AimToNearest(out float vx, out float vy, out float ang)
            {
                GetAimDirToNearest(out vx, out vy);
                ang = (float)Math.Atan2(vy, vx);
            }

            switch (id)
            {
                case "ab1":
                    {
                        int ticks = 10;
                        int interval = 300;
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab1"));
                        for (int i = 0; i < ticks; i++)
                        {
                            int delay = i * interval;
                            effects.Add(new DelayedAction(delay, () =>
                            {
                                effects.Add(new RingPulse(px, py, 70 + i * 4, 240, y1, y2, en => DealSkillDamage(en, dmg)));
                                Burst(px, py, 14, 0.05f, 0.22f, hot);
                            }));
                        }
                        break;
                    }

                case "ab2":
                    {
                        int duration = 6000, tick = 1000;
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab2"));
                        effects.Add(new ConePulse(player, 90, 160, duration, tick, y1, (en) => DealSkillDamage(en, dmg)));
                        break;
                    }

                case "ab3":
                    {
                        int dmg = (int)(5 * player.SkillDamageMult * GetAbilityDamageMult("ab3"));
                        AimToNearest(out var vx, out var vy, out _);
                        effects.Add(new RingPulse(px + vx * 22, py + vy * 22, 26, 140, white, y2, null));
                        FireTargetedBullet(dmg, pierce: 2, chain: 0, speed: 9f);
                        Burst(px, py, 12, 0.05f, 0.18f, y2);
                        break;
                    }

                case "ab4":
                    {
                        int dmg = (int)(6 * player.SkillDamageMult * GetAbilityDamageMult("ab4"));
                        effects.Add(new ArcSlash(player, 180, 120, 260, y1, en => DealSkillDamage(en, dmg)));
                        Burst(px, py, 18, 0.06f, 0.24f, hot);
                        break;
                    }

                case "ab5":
                    {
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab5"));
                        effects.Add(new RingPulse(px, py, 50, 160, y2, white, null));
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = (float)(i * (Math.PI * 2 / 8));
                            float vx = (float)Math.Cos(ang), vy = (float)Math.Sin(ang);
                            bullets.Add(new Bullet
                            {
                                X = px,
                                Y = py,
                                VX = vx,
                                VY = vy,
                                Speed = 7.5f,
                                Damage = dmg,
                                LifeMs = 1200,
                                Radius = 4,
                                Pierce = 0,
                                Chain = 0
                            });
                        }
                        Burst(px, py, 22, 0.05f, 0.20f, y2);
                        break;
                    }

                case "ab6":
                    {
                        int dmg = (int)(5 * player.SkillDamageMult * GetAbilityDamageMult("ab6"));
                        FireTargetedBullet(dmg, 0, 0, 10f, 6);
                        Burst(px, py, 14, 0.05f, 0.18f, y1);
                        break;
                    }

                case "ab7":
                    {
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab7"));
                        effects.Add(new ShieldBash(player, 70, 140, 220, y2, knockback: 30, onHit: en => DealSkillDamage(en, dmg)));
                        Burst(px, py, 14, 0.04f, 0.16f, y2);
                        break;
                    }

                case "ab8":
                    {
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab8"));
                        effects.Add(new RingSlash(px, py, 140, 260, y1, en => DealSkillDamage(en, dmg)));
                        Burst(px, py, 18, 0.06f, 0.22f, y1);
                        break;
                    }

                case "ab9":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float tx = t.X + t.W / 2f, ty = t.Y + t.H / 2f;
                        float dx = tx - px, dy = ty - py;
                        float len = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (len <= 0.0001f) break;
                        dx /= len; dy /= len;

                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab9"));
                        for (int i = -1; i <= 1; i++)
                        {
                            float a = (float)(Math.Atan2(dy, dx) + i * (Math.PI / 24));
                            bullets.Add(new Bullet
                            {
                                X = px,
                                Y = py,
                                VX = (float)Math.Cos(a),
                                VY = (float)Math.Sin(a),
                                Speed = 8.8f,
                                Damage = dmg,
                                LifeMs = 1300,
                                Radius = 4,
                                Pierce = 99,
                                Chain = 0
                            });
                            effects.Add(new ChainLine(px, py, px + (float)Math.Cos(a) * 36, py + (float)Math.Sin(a) * 36, 180));
                        }
                        break;
                    }

                case "ab10":
                    {
                        var t = NearestEnemy(px, py);
                        if (t != null)
                        {
                            ChainDamage(px, py, t, jumps: 4, dmg: (int)(2 * player.SkillDamageMult * GetAbilityDamageMult("ab10")));
                            Burst(px, py, 10, 0.04f, 0.14f, white);
                        }
                        break;
                    }

                case "ab11":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float cx = t.X + t.W / 2f, cy = t.Y + t.H / 2f;
                        int tickDmg = (int)(2 * player.SkillDamageMult * GetAbilityDamageMult("ab11"));
                        int explodeDmg = (int)(8 * player.SkillDamageMult * GetAbilityDamageMult("ab11"));

                        TelegraphCircle(cx, cy, 80, 300, y2);
                        effects.Add(new GroundZone(cx, cy, 80, 3000, 700, y2,
                            (en) => { DealSkillDamage(en, tickDmg); Burst(cx, cy, 6, 0.03f, 0.12f, y2); },
                            onEnd: () => effects.Add(new Explosion(cx, cy, 120, 360, y1, (en) => DealSkillDamage(en, explodeDmg)))
                        ));
                        break;
                    }

                case "ab12":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float cx = t.X + t.W / 2f, cy = t.Y + t.H / 2f;
                        TelegraphCircle(cx, cy, 70, 220, y2);
                        effects.Add(new VoidKill(cx, cy, 70, 420, y1, (en) => en.HP = 0));
                        Burst(cx, cy, 24, 0.04f, 0.22f, y1);
                        break;
                    }

                case "ab13":
                    {
                        effects.Add(new BuffTimer(10000,
                            onStart: () =>
                            {
                                player.SkillDamageMult *= 1.5f;
                                effects.Add(new RingPulse(px, py, 100, 300, white, y1, null));
                                Burst(px, py, 26, 0.05f, 0.18f, white);
                            },
                            onEnd: () => player.SkillDamageMult /= 1.5f));
                        break;
                    }

                case "ab14":
                    {
                        int dmg = (int)(50 * player.SkillDamageMult * GetAbilityDamageMult("ab14"));
                        effects.Add(new Explosion(px, py, 180, 420, y1, (en) => DealSkillDamage(en, dmg)));
                        effects.Add(new DelayedAction(90, () => effects.Add(new Explosion(px, py, 220, 380, y2, (en) => { }))));
                        Burst(px, py, 40, 0.06f, 0.28f, y1);
                        break;
                    }

                case "ab15":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;

                        float cx = t.X + t.W / 2f, cy = t.Y + t.H / 2f;
                        int dmg = (int)(2 * player.SkillDamageMult * GetAbilityDamageMult("ab15"));
                        int duration = 3000, interval = 300;
                        int ticks = duration / interval;
                        float areaR = 90f;

                        for (int i = 0; i < ticks; i++)
                        {
                            int delay = i * interval;
                            effects.Add(new DelayedAction(delay, () =>
                            {
                                float ang = (float)(rng.NextDouble() * Math.PI * 2);
                                float r = (float)(rng.NextDouble() * areaR);
                                float ax = cx + (float)Math.Cos(ang) * r;
                                float ay = cy + (float)Math.Sin(ang) * r;

                                TelegraphCircle(ax, ay, 16, 120, y2);
                                effects.Add(new DelayedAction(120, () =>
                                {
                                    effects.Add(new BeamStrike(ax, ay - 110, ay + 36, 160, y2, en => DealSkillDamage(en, dmg)));
                                    Burst(ax, ay, 8, 0.03f, 0.12f, y1);
                                }));
                            }));
                        }
                        break;
                    }

                case "ab16":
                    {
                        AimToNearest(out var dirx, out var diry, out var baseAng);
                        int dmg = (int)(2 * player.SkillDamageMult * GetAbilityDamageMult("ab16"));
                        int n = 20; float spread = 62f;
                        for (int i = 0; i < n; i++)
                        {
                            float a = (float)(baseAng + (Math.PI / 180.0) * (-spread / 2 + spread * (i / (float)(n - 1))));
                            bullets.Add(new Bullet
                            {
                                X = px,
                                Y = py,
                                VX = (float)Math.Cos(a),
                                VY = (float)Math.Sin(a),
                                Speed = 8.6f,
                                Damage = dmg,
                                LifeMs = 1300,
                                Radius = 4,
                                Pierce = 0,
                                Chain = 0
                            });
                        }
                        Burst(px, py, 18, 0.05f, 0.20f, y2);
                        break;
                    }

                case "ab17":
                    {
                        int dmg = (int)(8 * player.SkillDamageMult * GetAbilityDamageMult("ab17"));
                        AimToNearest(out var vx, out var vy, out var a);
                        effects.Add(new ChainLine(px, py, px + (float)Math.Cos(a) * 46, py + (float)Math.Sin(a) * 46, 200));
                        FireTargetedBullet(dmg, pierce: 99, chain: 0, speed: 10f, radius: 5);
                        Burst(px, py, 12, 0.03f, 0.12f, white);
                        break;
                    }

                case "ab18":
                    {
                        int duration = 8000, tick = 500;
                        int dmg = (int)(8 * player.SkillDamageMult * GetAbilityDamageMult("ab18"));
                        int count = duration / tick;
                        for (int i = 0; i < count; i++)
                        {
                            int delay = i * tick;
                            effects.Add(new DelayedAction(delay, () =>
                            {
                                float cx = rng.Next(60, ClientSize.Width - 60);
                                float cy = rng.Next(60, ClientSize.Height - 60);
                                TelegraphCircle(cx, cy, 18, 160, y2);
                                effects.Add(new DelayedAction(160, () =>
                                {
                                    effects.Add(new BeamStrike(cx, cy - 120, cy + 120, 180, y2, en => DealSkillDamage(en, dmg)));
                                    Burst(cx, cy, 10, 0.04f, 0.16f, y2);
                                }));
                            }));
                        }
                        break;
                    }

                case "ab19":
                    {
                        AimToNearest(out var vx, out var vy, out var baseAng);
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab19"));
                        float[] offs = { -7f, 0f, 7f };
                        for (int i = 0; i < offs.Length; i++)
                        {
                            float a = (float)(baseAng + (Math.PI / 180.0) * offs[i]);
                            bullets.Add(new Bullet
                            {
                                X = px,
                                Y = py,
                                VX = (float)Math.Cos(a),
                                VY = (float)Math.Sin(a),
                                Speed = 9.2f,
                                Damage = dmg,
                                LifeMs = 1200,
                                Radius = 4,
                                Pierce = 0,
                                Chain = 0
                            });
                            effects.Add(new ChainLine(px, py, px + (float)Math.Cos(a) * 28, py + (float)Math.Sin(a) * 28, 180));
                        }
                        break;
                    }

                case "ab20":
                    {
                        int dmg = (int)(15 * player.SkillDamageMult * GetAbilityDamageMult("ab20"));
                        FireTargetedBullet(dmg, 0, 1, 9.5f, 5);
                        Burst(px, py, 14, 0.05f, 0.18f, y2);
                        break;
                    }

                case "ab21":
                    {
                        int duration = 3000, interval = 200;
                        int shots = duration / interval;
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab21"));
                        for (int i = 0; i < shots; i++)
                        {
                            int delay = i * interval;
                            effects.Add(new DelayedAction(delay, () =>
                            {
                                float ang = (float)(rng.NextDouble() * Math.PI * 2);
                                float vx = (float)Math.Cos(ang), vy = (float)Math.Sin(ang);
                                bullets.Add(new Bullet
                                {
                                    X = px,
                                    Y = py,
                                    VX = vx,
                                    VY = vy,
                                    Speed = 9.0f,
                                    Damage = dmg,
                                    LifeMs = 1200,
                                    Radius = 4,
                                    Pierce = 0,
                                    Chain = 0
                                });
                                Burst(px, py, 6, 0.04f, 0.12f, y2);
                            }));
                        }
                        effects.Add(new RingPulse(px, py, 80, 220, y2, white, null));
                        break;
                    }

                case "ab22":
                    {
                        int swords = 5;
                        int dmg = (int)(5 * player.SkillDamageMult * GetAbilityDamageMult("ab22"));
                        effects.Add(new RingPulse(px, py, 90, 220, y1, white, null));

                        var visited = new System.Collections.Generic.HashSet<Enemy>();
                        for (int i = 0; i < swords; i++)
                        {
                            Enemy t = NearestEnemy(px, py, e => !visited.Contains(e));
                            if (t == null) t = NearestEnemy(px, py);
                            if (t != null) visited.Add(t);

                            float ang = (float)(i * (2 * Math.PI / swords));
                            float ox = (float)Math.Cos(ang) * 28f;
                            float oy = (float)Math.Sin(ang) * 28f;

                            float tx = t != null ? t.X + t.W / 2f : px + (float)Math.Cos(ang) * 100f;
                            float ty = t != null ? t.Y + t.H / 2f : py + (float)Math.Sin(ang) * 100f;

                            float dx = tx - (px + ox), dy = ty - (py + oy);
                            float len = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (len > 0.0001f) { dx /= len; dy /= len; } else { dx = (float)Math.Cos(ang); dy = (float)Math.Sin(ang); }

                            bullets.Add(new Bullet
                            {
                                X = px + ox,
                                Y = py + oy,
                                VX = dx,
                                VY = dy,
                                Speed = 9.5f,
                                Damage = dmg,
                                LifeMs = 1400,
                                Radius = 5,
                                Pierce = 0,
                                Chain = 0
                            });
                        }
                        Burst(px, py, 14, 0.05f, 0.18f, y1);
                        break;
                    }

                case "ab23":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float cx = t.X + t.W / 2f, cy = t.Y + t.H / 2f;
                        float r = 140f;
                        int baseDmg = (int)(20 * player.SkillDamageMult * GetAbilityDamageMult("ab23"));

                        TelegraphCircle(cx, cy, r * 0.8f, 220, y2);
                        effects.Add(new DelayedAction(220, () =>
                        {
                            effects.Add(new Explosion(cx, cy, r, 360, y1, en =>
                            {
                                float ex = en.X + en.W / 2f, ey = en.Y + en.H / 2f;
                                float dx = ex - cx, dy = ey - cy;
                                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (d > r) return;
                                float k = 1f - Math.Min(1f, d / r);
                                int dmg = Math.Max(1, (int)Math.Round(baseDmg * (0.35f + 0.65f * k)));
                                DealSkillDamage(en, dmg);
                            }));
                        }));
                        break;
                    }

                case "ab24":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float cx = t.X + t.W / 2f, cy = t.Y + t.H / 2f;
                        int duration = 2000, tick = 500;
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab24"));
                        effects.Add(new GroundZone(cx, cy, 60f, duration, tick, y2, en => DealSkillDamage(en, dmg), null));
                        effects.Add(new RingPulse(cx, cy, 60, 240, white, y2, null));
                        break;
                    }

                case "ab25":
                    {
                        int swords = 2;
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab25"));
                        effects.Add(new RingPulse(px, py, 70, 200, y1, white, null));

                        var visited = new System.Collections.Generic.HashSet<Enemy>();
                        for (int i = 0; i < swords; i++)
                        {
                            Enemy t = NearestEnemy(px, py, e => !visited.Contains(e));
                            if (t == null) t = NearestEnemy(px, py);
                            if (t != null) visited.Add(t);

                            float ang = (float)(i * Math.PI);
                            float ox = (float)Math.Cos(ang) * 20f;
                            float oy = (float)Math.Sin(ang) * 20f;

                            float tx = t != null ? t.X + t.W / 2f : px + (float)Math.Cos(ang) * 100f;
                            float ty = t != null ? t.Y + t.H / 2f : py + (float)Math.Sin(ang) * 100f;

                            float dx = tx - (px + ox), dy = ty - (py + oy);
                            float len = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (len > 0.0001f) { dx /= len; dy /= len; } else { dx = (float)Math.Cos(ang); dy = (float)Math.Sin(ang); }

                            bullets.Add(new Bullet
                            {
                                X = px + ox,
                                Y = py + oy,
                                VX = dx,
                                VY = dy,
                                Speed = 9.6f,
                                Damage = dmg,
                                LifeMs = 1200,
                                Radius = 5,
                                Pierce = 0,
                                Chain = 0
                            });
                        }
                        Burst(px, py, 10, 0.04f, 0.16f, y2);
                        break;
                    }

                case "ab26":
                    {
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab26"));
                        float pull = 42f;
                        foreach (var e in enemies)
                        {
                            float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                            float dx = px - ex, dy = py - ey;
                            float len = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (len > 0.0001f) { dx /= len; dy /= len; }
                            e.X += dx * pull;
                            e.Y += dy * pull;
                            DealSkillDamage(e, dmg);
                        }
                        effects.Add(new RingPulse(px, py, 140, 260, y2, white, null));
                        Burst(px, py, 18, 0.05f, 0.18f, y2);
                        break;
                    }

                case "ab27":
                    {
                        AimToNearest(out var vx, out var vy, out var a0);
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab27"));
                        float[] offs = { -15f, 15f };
                        foreach (var off in offs)
                        {
                            float a = (float)(a0 + off * Math.PI / 180.0);
                            bullets.Add(new Bullet
                            {
                                X = px,
                                Y = py,
                                VX = (float)Math.Cos(a),
                                VY = (float)Math.Sin(a),
                                Speed = 9.0f,
                                Damage = dmg,
                                LifeMs = 1200,
                                Radius = 4,
                                Pierce = 0,
                                Chain = 0
                            });
                            effects.Add(new ChainLine(px, py, px + (float)Math.Cos(a) * 30, py + (float)Math.Sin(a) * 30, 180));
                        }
                        break;
                    }

                case "ab28":
                    {
                        var t = NearestEnemy(px, py);
                        if (t == null) break;
                        float ex = t.X + t.W / 2f, ey = t.Y + t.H / 2f;
                        float dx = ex - px, dy = ey - py;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d <= 110f)
                        {
                            if (d > 0.0001f) { dx /= d; dy /= d; }
                            int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab28"));
                            DealSkillDamage(t, dmg);
                            float push = 600f;
                            t.X = Math.Max(0, Math.Min(ClientSize.Width - t.W, t.X + dx * push));
                            t.Y = Math.Max(0, Math.Min(ClientSize.Height - t.H, t.Y + dy * push));
                            effects.Add(new RingPulse(px, py, 80, 220, y1, white, null));
                            Burst(px, py, 10, 0.04f, 0.14f, y1);
                        }
                        break;
                    }

                case "ab29":
                    {
                        int dmg = (int)(4 * player.SkillDamageMult * GetAbilityDamageMult("ab29"));
                        effects.Add(new ChaoticOrb(px, py, speed: 8.6f, radius: 6f, lifeMs: 1400, c: Color.FromArgb(255, 235, 160), damage: dmg));
                        break;
                    }

                case "ab30":
                    {
                        int duration = 5000, tick = 300;
                        int dmg = (int)(3 * player.SkillDamageMult * GetAbilityDamageMult("ab30"));
                        effects.Add(new PlayerAura(player, radius: 110f, durationMs: duration, tickMs: tick, c: y2, onTick: en => DealSkillDamage(en, dmg)));
                        effects.Add(new RingPulse(px, py, 110, 260, white, y2, null));
                        break;
                    }
            }
        }
            private class ChaoticOrb : EffectBase
        {
            private float x, y, vx, vy, speed, radius;
            private readonly Color c;
            private readonly int life0;
            private readonly int damage;
            private float angle;
            private bool dealt = false;

            public ChaoticOrb(float x, float y, float speed, float radius, int lifeMs, Color c, int damage)
            {
                this.x = x; this.y = y; this.speed = speed; this.radius = radius; this.c = c; this.damage = damage;
                LifeMs = lifeMs; life0 = lifeMs;
                angle = (float)(new Random().NextDouble() * Math.PI * 2);
                vx = (float)Math.Cos(angle);
                vy = (float)Math.Sin(angle);
            }

            public override void Update(GameForm G, int dt)
            {
                float jitter = 0.20f;
                angle += (float)((G.rng.NextDouble() - 0.5) * jitter);
                vx = (float)Math.Cos(angle);
                vy = (float)Math.Sin(angle);

                x += vx * speed * (dt / 16f);
                y += vy * speed * (dt / 16f);

                foreach (var e in G.enemies)
                {
                    var r = new RectangleF(x - radius, y - radius, radius * 2, radius * 2);
                    if (r.IntersectsWith(new RectangleF(e.X, e.Y, e.W, e.H)))
                    {
                        if (!dealt)
                        {
                            G.DealSkillDamage(e, damage);
                            dealt = true;
                        }
                        LifeMs = Math.Min(LifeMs, 120);
                    }
                }

                base.Update(G, dt);
            }

                 public override void Draw(Graphics g)
            {
                float a = Math.Max(0, Math.Min(1, LifeMs / (float)life0));

                float glowR = radius * 3.0f;
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(x - glowR, y - glowR, glowR * 2, glowR * 2);
                    using var pgb = new PathGradientBrush(gp)
                    {
                        CenterColor = Color.FromArgb((int)(120 * a), c),
                        SurroundColors = new[] { Color.FromArgb(0, c) }
                    };
                    g.FillPath(pgb, gp);
                }

                using (var br = new SolidBrush(Color.FromArgb((int)(230 * a), c)))
                    g.FillEllipse(br, x - radius, y - radius, radius * 2, radius * 2);

                using (var pen = new Pen(Color.FromArgb((int)(200 * a), 255, 255, 240), 1.5f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawEllipse(pen, x - radius, y - radius, radius * 2, radius * 2);
                }
            }
        }

        private class PlayerAura : EffectBase
        {
            private readonly Player p;
            private readonly float radius;
            private readonly int tick;
            private int accum;
            private readonly Color c;
            private readonly Action<Enemy> onTick;

            public PlayerAura(Player p, float radius, int durationMs, int tickMs, Color c, Action<Enemy> onTick)
            {
                this.p = p;
                this.radius = radius;
                this.tick = tickMs;
                this.c = c;
                this.onTick = onTick;
                LifeMs = durationMs;
                accum = 0;
            }

            public override void Update(GameForm G, int dt)
            {
                accum += dt;
                while (accum >= tick)
                {
                    accum -= tick;
                    float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;
                    foreach (var e in G.enemies)
                    {
                        float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                        if (Dist2(cx, cy, ex, ey) <= radius * radius) onTick?.Invoke(e);
                    }
                }
                base.Update(G, dt);
            }

            public override void Draw(Graphics g)
            {
                float cx = p.X + p.W / 2f, cy = p.Y + p.H / 2f;

                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
                    using var glow = new PathGradientBrush(gp)
                    {
                        CenterColor = Color.FromArgb(80, c),
                        SurroundColors = new[] { Color.FromArgb(0, c) }
                    };
                    g.FillPath(glow, gp);
                }

                using var pen = new Pen(Color.FromArgb(160, c), 2f);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            }
        }
    }
}
