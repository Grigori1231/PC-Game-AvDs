using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Rogalik
{
    public partial class GameForm
    {
        private enum Facing { Down, Up, Left, Right }
        private enum EnemyKind { Common, Strong, Archer, Mage }

        private class Player
        {
            public float X, Y, W, H;
            public SpriteSet Sprites;
            public Facing Facing;
            public bool Moving;

            public int MaxHP, HP;
            public float BaseSpeed, SpeedMult;

            public int Damage;
            public double AttackCooldownMs;
            public float ProjectileSpeed;
            public int ProjectileCount;

            public float CritChance, CritMult;

            public float SkillDamageMult;
            public float XpGainMult;

            public float Armor;
        }

        private class Enemy
        {
            public float X, Y, W, H;
            public SpriteSet Sprites;
            public Facing Facing;
            public bool Moving;

            public EnemyKind Kind;

            public int MaxHP, HP;
            public float BaseSpeed;
            public int ContactDamage;

            public double AttackCooldownMs;
            public DateTime LastAttackTime;

            public double ShootCooldownMs;
            public DateTime LastShootTime;
            public float PreferredRange;
            public int RangedDamage;

            public bool IsCasting;
            public int CastLeftMs;
            public int CastWindupMs;
        }

        private class Bullet
        {
            public float X, Y;
            public float VX, VY;
            public float Speed;
            public int Damage;
            public int LifeMs;
            public float Radius;
            public int Pierce;
            public int Chain;
        }

        private enum EnemyProjectileKind { Arrow, MageOrb }

        private class EnemyProjectile
        {
            public float X, Y;
            public float VX, VY;
            public float Speed;
            public int Damage;
            public int LifeMs;
            public float Radius = 4f;
            public System.Drawing.Image Image;
            public float AngleRad;

            public int HomingMsLeft = 0;
            public EnemyProjectileKind Kind = EnemyProjectileKind.Arrow;
        }

        private readonly Dictionary<string, int> buffLevels = new();
        private int levelUpStep = 0;

        private void CreatePlayer()
        {
            angelSprites ??= LoadAngelSprites();

            int size = PlayerSizePx * SpriteScale;

            player = new Player
            {
                X = ClientSize.Width / 2f - size / 2f,
                Y = ClientSize.Height / 2f - size / 2f,
                W = size,
                H = size,
                Sprites = angelSprites,
                Facing = Facing.Down,
                MaxHP = 120,
                HP = 120,
                BaseSpeed = 3.2f,
                SpeedMult = 1.0f,
                Damage = 3,
                AttackCooldownMs = 600,
                ProjectileSpeed = 7.0f,
                ProjectileCount = 1,
                CritChance = 0.05f,
                CritMult = 1.5f,
                SkillDamageMult = 1.0f,
                XpGainMult = 1.0f,
                Armor = 0f
            };

            abilityLevels.Clear();
            abilityOrder.Clear();
            cdLeftMs.Clear();
            buffLevels.Clear();
            levelUpStep = 0;

            enemies.Clear();
            bullets.Clear();
            enemyProjectiles.Clear();

            level = 1;
            xp = 0;
            xpToNext = 30;

            keyW = keyA = keyS = keyD = false;

            UpdateHpUi();
        }

        private SpriteSet demonStrongSprites, demonArcherSprites, demonMageSprites;

        private Image archerArrowUp, archerArrowDown, archerArrowLeft, archerArrowRight;
        private Image archerArrowGeneric;

        private void EnsureArcherArrowImages()
        {
            if (archerArrowUp != null || archerArrowDown != null || archerArrowLeft != null || archerArrowRight != null) return;

            try
            {
                string root = FindGifsRoot();
                string folder = Path.Combine(root, "demon_archer");
                if (!Directory.Exists(folder)) return;

                Image TryLoad(string stem)
                {
                    string[] exts = { ".png", ".gif", ".jpg", ".jpeg" };
                    foreach (var ext in exts)
                    {
                        var p = Path.Combine(folder, stem + ext);
                        if (File.Exists(p)) return Image.FromFile(p);
                    }
                    var found = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(f =>
                        {
                            var n = Path.GetFileNameWithoutExtension(f);
                            var e = Path.GetExtension(f).ToLowerInvariant();
                            return n.Equals(stem, StringComparison.OrdinalIgnoreCase)
                                   && (e == ".png" || e == ".gif" || e == ".jpg" || e == ".jpeg");
                        });
                    return found != null ? Image.FromFile(found) : null;
                }

                archerArrowUp = TryLoad("arrow_up");
                archerArrowDown = TryLoad("arrow_down");
                archerArrowLeft = TryLoad("arrow_left");
                archerArrowRight = TryLoad("arrow_right");

                if (archerArrowUp == null && archerArrowDown == null && archerArrowLeft == null && archerArrowRight == null)
                {
                    var any = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(f => Path.GetFileName(f).IndexOf("arrow", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (any != null) archerArrowGeneric = Image.FromFile(any);
                }
            }
            catch { }
        }

        private void SpawnCommonEnemy()
        {
            demonSprites ??= LoadDemonCommonSprites();
            SpawnEnemyBase(EnemyKind.Common);
        }

        public void SpawnStrongEnemy()
        {
            demonStrongSprites ??= LoadDemonStrongSprites();
            SpawnEnemyBase(EnemyKind.Strong);
        }

        public void SpawnArcherEnemy()
        {
            demonArcherSprites ??= LoadDemonArcherSprites();
            EnsureArcherArrowImages();
            SpawnEnemyBase(EnemyKind.Archer);
        }

        public void SpawnMageEnemy()
        {
            demonMageSprites ??= LoadDemonMageSprites();
            SpawnEnemyBase(EnemyKind.Mage);
        }

        private void SpawnEnemyBase(EnemyKind kind)
        {
            int size = EnemySizePx * SpriteScale;

            int side = rng.Next(4);
            float x = 0, y = 0;
            switch (side)
            {
                case 0: x = -size; y = rng.Next(0, ClientSize.Height - size); break;
                case 1: x = ClientSize.Width; y = rng.Next(0, ClientSize.Height - size); break;
                case 2: x = rng.Next(0, ClientSize.Width - size); y = -size; break;
                case 3: x = rng.Next(0, ClientSize.Width - size); y = ClientSize.Height; break;
            }

            var en = new Enemy
            {
                X = x,
                Y = y,
                W = size,
                H = size,
                Facing = Facing.Down,
                Kind = kind,
                LastAttackTime = DateTime.UtcNow,
                LastShootTime = DateTime.UtcNow
            };

            switch (kind)
            {
                case EnemyKind.Common:
                    en.Sprites = demonSprites ?? LoadDemonCommonSprites();
                    en.MaxHP = en.HP = 10;
                    en.BaseSpeed = 2.2f;
                    en.ContactDamage = 10;
                    en.AttackCooldownMs = 550;
                    break;

                case EnemyKind.Strong:
                    en.Sprites = demonStrongSprites ?? demonSprites ?? LoadDemonCommonSprites();
                    en.MaxHP = en.HP = 15;
                    en.BaseSpeed = 1.6f;
                    en.ContactDamage = 10;
                    en.AttackCooldownMs = 700;
                    break;

                case EnemyKind.Archer:
                    en.Sprites = demonArcherSprites ?? demonSprites ?? LoadDemonCommonSprites();
                    en.MaxHP = en.HP = 7;
                    en.BaseSpeed = 2.0f;
                    en.ContactDamage = 6;
                    en.AttackCooldownMs = 650;

                    en.ShootCooldownMs = 4000;
                    en.PreferredRange = 300;
                    en.RangedDamage = 5;

                    en.IsCasting = false;
                    en.CastWindupMs = 1000;
                    en.CastLeftMs = 0;
                    break;

                case EnemyKind.Mage:
                    en.Sprites = demonMageSprites ?? demonSprites ?? LoadDemonCommonSprites();
                    en.MaxHP = en.HP = 5;
                    en.BaseSpeed = 1.5f;
                    en.ContactDamage = 10;
                    en.AttackCooldownMs = 700;

                    en.ShootCooldownMs = 3000;
                    en.PreferredRange = 320f;
                    en.RangedDamage = 12;

                    en.IsCasting = false;
                    en.CastWindupMs = 0;
                    en.CastLeftMs = 0;
                    break;
            }

            enemies.Add(en);
        }

        private readonly List<EnemyProjectile> enemyProjectiles = new();

        private void UpdateEnemies(int dt)
        {
            if (picker != null && picker.IsOpen) return;

            var now = DateTime.UtcNow;
            float px = player.X + player.W / 2f, py = player.Y + player.H / 2f;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];

                float ex = e.X + e.W / 2f, ey = e.Y + e.H / 2f;
                float dx = px - ex, dy = py - ey;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);

                float move = (float)(e.BaseSpeed * dt / 16.0f);
                bool moving = false;

                if (e.Kind == EnemyKind.Archer)
                {
                    if (Math.Abs(dx) > Math.Abs(dy))
                        e.Facing = dx > 0 ? Facing.Right : Facing.Left;
                    else
                        e.Facing = dy > 0 ? Facing.Down : Facing.Up;

                    if (e.IsCasting)
                    {
                        e.CastLeftMs -= dt;
                        if (e.CastLeftMs <= 0)
                        {
                            double baseAng = e.Facing switch
                            {
                                Facing.Left => Math.PI,
                                Facing.Right => 0.0,
                                Facing.Up => -Math.PI / 2.0,
                                _ => Math.PI / 2.0
                            };
                            double devDeg = 6.0;
                            double devRad = (rng.NextDouble() * 2 - 1) * (devDeg * Math.PI / 180.0);
                            double a = baseAng + devRad;

                            float vx = (float)Math.Cos(a);
                            float vy = (float)Math.Sin(a);

                            Image img = e.Facing switch
                            {
                                Facing.Left => archerArrowLeft ?? archerArrowGeneric,
                                Facing.Right => archerArrowRight ?? archerArrowGeneric,
                                Facing.Up => archerArrowUp ?? archerArrowGeneric,
                                _ => archerArrowDown ?? archerArrowGeneric,
                            };

                            enemyProjectiles.Add(new EnemyProjectile
                            {
                                X = ex,
                                Y = ey,
                                VX = vx,
                                VY = vy,
                                Speed = 8.0f,
                                Damage = e.RangedDamage,
                                LifeMs = 2200,
                                Radius = 5f,
                                Image = img,
                                AngleRad = (float)a
                            });

                            e.IsCasting = false;
                            e.CastLeftMs = 0;
                            e.LastShootTime = now;
                        }
                    }
                    else
                    {
                        const float buffer = 40f;
                        if (len > e.PreferredRange + buffer && len > 0.0001f)
                        {
                            float ndx = dx / len, ndy = dy / len;
                            e.X += ndx * move; e.Y += ndy * move;
                            moving = true;
                        }
                        else if (len < e.PreferredRange - buffer && len > 0.0001f)
                        {
                            float ndx = -dx / len, ndy = -dy / len;
                            e.X += ndx * move; e.Y += ndy * move;
                            moving = true;
                        }

                        if ((now - e.LastShootTime).TotalMilliseconds >= e.ShootCooldownMs)
                        {
                            e.IsCasting = true;
                            e.CastLeftMs = e.CastWindupMs;
                        }
                    }
                }
                else if (e.Kind == EnemyKind.Mage)
                {
                    if (Math.Abs(dx) > Math.Abs(dy)) e.Facing = dx > 0 ? Facing.Right : Facing.Left;
                    else e.Facing = dy > 0 ? Facing.Down : Facing.Up;

                    if (e.IsCasting)
                    {
                        e.CastLeftMs -= dt;
                        if (e.CastLeftMs <= 0) e.IsCasting = false;
                    }

                    const float buffer = 50f;
                    if (len > e.PreferredRange + buffer && len > 0.0001f)
                    {
                        float ndx = dx / len, ndy = dy / len;
                        e.X += ndx * move; e.Y += ndy * move; moving = true;
                    }
                    else if (len < e.PreferredRange - buffer && len > 0.0001f)
                    {
                        float ndx = -dx / len, ndy = -dy / len;
                        e.X += ndx * move; e.Y += ndy * move; moving = true;
                    }

                    if ((now - e.LastShootTime).TotalMilliseconds >= e.ShootCooldownMs)
                    {
                        float dirx = 0, diry = 1;
                        if (len > 0.0001f) { dirx = dx / len; diry = dy / len; }

                        enemyProjectiles.Add(new EnemyProjectile
                        {
                            Kind = EnemyProjectileKind.MageOrb,
                            X = ex,
                            Y = ey,
                            VX = dirx,
                            VY = diry,
                            Speed = 5.0f,
                            Damage = e.RangedDamage,
                            LifeMs = 12000,
                            Radius = 8f,
                            Image = null,
                            AngleRad = (float)Math.Atan2(diry, dirx),
                            HomingMsLeft = 2000
                        });

                        e.IsCasting = true;
                        e.CastLeftMs = 300;
                        e.LastShootTime = now;
                    }
                }
                else
                {
                    if (len > 0.0001f)
                    {
                        float ndx = dx / len, ndy = dy / len;
                        e.X += ndx * move; e.Y += ndy * move;
                        moving = true;

                        if (Math.Abs(ndx) > Math.Abs(ndy))
                            e.Facing = ndx > 0 ? Facing.Right : Facing.Left;
                        else
                            e.Facing = ndy > 0 ? Facing.Down : Facing.Up;
                    }
                }

                e.Moving = moving;

                if (Rect(e).IntersectsWith(Rect(player)))
                {
                    if ((now - e.LastAttackTime).TotalMilliseconds >= e.AttackCooldownMs)
                    {
                        e.LastAttackTime = now;
                        ApplyDamageToPlayer(e.ContactDamage);
                    }
                }

                if (e.HP <= 0)
                {
                    int baseXp = 10;
                    float mult = e.Kind switch
                    {
                        EnemyKind.Strong => 2.0f,
                        EnemyKind.Archer => 1.5f,
                        EnemyKind.Mage => 1.6f,
                        _ => 1.0f
                    };
                    int reward = (int)Math.Round(baseXp * mult * player.XpGainMult);

                    enemies.RemoveAt(i);
                    killCount++;
                    GainXP(reward);
                }
            }

            UpdateEnemyProjectiles(dt);
        }

        private void UpdateEnemyProjectiles(int dt)
        {
            if (picker != null && picker.IsOpen) return;

            for (int i = enemyProjectiles.Count - 1; i >= 0; i--)
            {
                var p = enemyProjectiles[i];

                if (p.Kind == EnemyProjectileKind.MageOrb && p.HomingMsLeft > 0)
                {
                    p.HomingMsLeft -= dt;
                    float tx = player.X + player.W / 2f, ty = player.Y + player.H / 2f;
                    float ddx = tx - p.X, ddy = ty - p.Y;
                    float dlen = (float)Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (dlen > 0.0001f)
                    {
                        ddx /= dlen; ddy /= dlen;
                        float w = Math.Max(0.04f, Math.Min(0.18f, 0.06f * (dt / 16f)));
                        p.VX = p.VX + (ddx - p.VX) * w;
                        p.VY = p.VY + (ddy - p.VY) * w;
                        float n = (float)Math.Sqrt(p.VX * p.VX + p.VY * p.VY);
                        if (n > 0.0001f) { p.VX /= n; p.VY /= n; }
                        p.AngleRad = (float)Math.Atan2(p.VY, p.VX);
                    }
                }

                p.LifeMs -= dt;
                p.X += p.VX * p.Speed * (float)(dt / 16.0f);
                p.Y += p.VY * p.Speed * (float)(dt / 16.0f);

                bool outOf = p.LifeMs <= 0
                    || p.X < -80 || p.X > ClientSize.Width + 80
                    || p.Y < -80 || p.Y > ClientSize.Height + 80;

                if (outOf)
                {
                    enemyProjectiles.RemoveAt(i);
                    continue;
                }

                var pr = new RectangleF(p.X - p.Radius, p.Y - p.Radius, p.Radius * 2, p.Radius * 2);
                if (pr.IntersectsWith(Rect(player)))
                {
                    ApplyDamageToPlayer(p.Damage);
                    enemyProjectiles.RemoveAt(i);
                }
            }
        }

        private void UpdateBullets(int dt)
        {
            if (picker != null && picker.IsOpen) return;

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                b.LifeMs -= dt;
                b.X += b.VX * b.Speed * (float)(dt / 16.0f);
                b.Y += b.VY * b.Speed * (float)(dt / 16.0f);

                if (b.LifeMs <= 0 || b.X < -50 || b.X > ClientSize.Width + 50 || b.Y < -50 || b.Y > ClientSize.Height + 50)
                {
                    bullets.RemoveAt(i);
                    continue;
                }

                bool hit = false;
                var bRect = new RectangleF(b.X - b.Radius, b.Y - b.Radius, b.Radius * 2, b.Radius * 2);

                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    var e = enemies[j];
                    if (bRect.IntersectsWith(Rect(e)))
                    {
                        bool isCrit = false;
                        int dmg = b.Damage;
                        if (rng.NextDouble() < player.CritChance)
                        {
                            isCrit = true;
                            dmg = (int)Math.Round(dmg * player.CritMult);
                        }
                        e.HP -= dmg;
                        if (e.HP < 0) e.HP = 0;

                        SpawnDamageNumber(e, dmg, isCrit);

                        if (b.Chain > 0)
                        {
                            var n = NearestEnemy(e.X + e.W / 2f, e.Y + e.H / 2f, except: e);
                            if (n != null)
                            {
                                float sx = e.X + e.W / 2f, sy = e.Y + e.H / 2f;
                                float tx = n.X + n.W / 2f, ty = n.Y + n.H / 2f;
                                float dx = tx - sx, dy = ty - sy;
                                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (len > 0.0001f) { dx /= len; dy /= len; }
                                bullets.Add(new Bullet
                                {
                                    X = sx,
                                    Y = sy,
                                    VX = dx,
                                    VY = dy,
                                    Speed = b.Speed,
                                    Damage = b.Damage,
                                    LifeMs = 1000,
                                    Radius = b.Radius,
                                    Pierce = 0,
                                    Chain = b.Chain - 1
                                });
                            }
                        }

                        if (b.Pierce > 0) { b.Pierce--; hit = false; }
                        else { hit = true; }
                        if (hit) break;
                    }
                }

                if (hit) bullets.RemoveAt(i);
            }
        }

        private SpriteSet LoadDemonStrongSpritesFallback() => LoadDemonCommonSprites();
        private SpriteSet LoadDemonArcherSpritesFallback() => LoadDemonCommonSprites();

        public void SpawnAnyEnemyRandom()
        {
            int t = rng.Next(4);
            if (t == 0) SpawnCommonEnemy();
            else if (t == 1) SpawnStrongEnemy();
            else if (t == 2) SpawnArcherEnemy();
            else SpawnMageEnemy();
        }
    }
}