using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Rogalik
{
    public partial class GameForm : Form
    {
        private static readonly string GifsRoot = FindGifsRoot();

        private static string FindGifsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
            Path.Combine(baseDir, "gifs_animation"),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\gifs_animation")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\gifs_animation")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\gifs_animation")),
        };
            foreach (var c in candidates) if (Directory.Exists(c)) return c;
            return Path.Combine(baseDir, "gifs_animation");
        }

        private static string G(string relative) => Path.Combine(GifsRoot, relative);

        private class Anim
        {
            public readonly System.Collections.Generic.List<Bitmap> Frames = new();
            public readonly System.Collections.Generic.List<int> DelayMs = new();
            public int Index = 0;
            private int accum = 0;
            public bool IsEmpty => Frames.Count == 0;

            public void Update(int dt)
            {
                if (Frames.Count <= 1) return;
                accum += dt;
                while (accum >= DelayMs[Index])
                {
                    accum -= DelayMs[Index];
                    Index++;
                    if (Index >= Frames.Count) Index = 0;
                }
            }

            public Bitmap Cur => IsEmpty ? null : Frames[Index];
        }

        private static Anim LoadAnim(Size target, string ph, params string[] rels)
        {
            foreach (var rel in rels)
            {
                var path = G(rel);
                if (!File.Exists(path)) continue;
                try
                {
                    using var img = Image.FromFile(path);
                    var dim = FrameDimension.Time;
                    int count;
                    try { count = img.GetFrameCount(dim); }
                    catch { count = 1; }

                    var a = new Anim();
                    if (count <= 1)
                    {
                        a.Frames.Add(ScaleTo(img, target));
                        a.DelayMs.Add(100);
                        return a;
                    }

                    int[] delays = GetGifDelays(img, count);
                    for (int i = 0; i < count; i++)
                    {
                        img.SelectActiveFrame(dim, i);
                        using var tmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppPArgb);
                        using (var gg = Graphics.FromImage(tmp))
                        {
                            gg.CompositingMode = CompositingMode.SourceOver;
                            gg.Clear(Color.Transparent);
                            gg.DrawImage(img, 0, 0);
                        }
                        var sc = ScaleTo(tmp, target);
                        a.Frames.Add(sc);
                        a.DelayMs.Add(Math.Max(MinFrameMs, delays[i] * 10));
                    }
                    return a;
                }
                catch { }
            }

            var phBmp = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(phBmp))
            {
                g.Clear(Color.FromArgb(60, 60, 68));
                using var br = new SolidBrush(Color.White);
                using var font = new Font("Segoe UI", 7);
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(ph, font, br, new RectangleF(0, 0, target.Width, target.Height), fmt);
            }
            var a2 = new Anim();
            a2.Frames.Add(phBmp);
            a2.DelayMs.Add(200);
            return a2;
        }

        private static int[] GetGifDelays(Image img, int frameCount)
        {
            try
            {
                var item = img.GetPropertyItem(0x5100);
                var vals = new int[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    vals[i] = BitConverter.ToInt32(item.Value, i * 4);
                    if (vals[i] <= 0) vals[i] = 10;
                }
                return vals;
            }
            catch { return Enumerable.Repeat(10, frameCount).ToArray(); }
        }

        private static Bitmap ScaleTo(Image src, Size target)
        {
            var bmp = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode.SourceOver;
            g.Clear(Color.Transparent);
            g.DrawImage(src, new Rectangle(Point.Empty, target));
            return bmp;
        }

        private class SpriteSet
        {
            public Anim IdleUp, IdleDown, IdleLeft, IdleRight;
            public Anim WalkUp, WalkDown, WalkLeft, WalkRight;
            public Anim AttackUp, AttackDown, AttackLeft, AttackRight;
            public Anim Death;

            public static Anim FirstNonNull(params Anim[] arr) =>
                arr.FirstOrDefault(a => a != null && !a.IsEmpty) ?? arr.FirstOrDefault(a => a != null) ?? new Anim();
        }

        private SpriteSet LoadAngelSprites() => new SpriteSet
        {
            IdleDown = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "idle down", @"angel\afk_down_ang.png", @"angel\idle_down.png"),
            IdleUp = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "idle up", @"angel\afk_up_ang.png", @"angel\idle_up.png"),
            IdleLeft = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "idle left", @"angel\afk_left_ang.png", @"angel\idle_left.png"),
            IdleRight = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "idle right", @"angel\afk_right_ang.png", @"angel\idle_right.png"),

            WalkUp = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "walk up", @"angel\walk_up.gif"),
            WalkDown = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "walk down", @"angel\walk_down.gif"),
            WalkLeft = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "walk left", @"angel\walk_left.gif"),
            WalkRight = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "walk right", @"angel\walk_right.gif"),

            Death = LoadAnim(new Size(PlayerSizePx * SpriteScale, PlayerSizePx * SpriteScale), "death", @"angel\death_ang.gif", @"angel\death.gif")
        };

        private SpriteSet LoadDemonCommonSprites() => new SpriteSet
        {
            IdleDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "idle", @"demon_common\walk_down_d.gif", @"demon_common\idle_down_d.gif"),
            IdleUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "idle", @"demon_common\walk_up_d.gif", @"demon_common\idle_up_d.gif"),
            IdleLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "idle", @"demon_common\walk_left_d.gif", @"demon_common\idle_left_d.gif"),
            IdleRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "idle", @"demon_common\walk_right_d.gif", @"demon_common\idle_right_d.gif"),

            WalkUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "up", @"demon_common\walk_up_d.gif"),
            WalkDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "down", @"demon_common\walk_down_d.gif"),
            WalkLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "left", @"demon_common\walk_left_d.gif"),
            WalkRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "right", @"demon_common\walk_right_d.gif")
        };

        private SpriteSet LoadDemonStrongSprites() => new SpriteSet
        {
            IdleDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong idle down", @"demon_strong\walk_strong_down.gif"),
            IdleUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong idle up", @"demon_strong\walk_strong_up.gif"),
            IdleLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong idle left", @"demon_strong\walk_strong_left.gif"),
            IdleRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong idle right", @"demon_strong\walk_strong_right.gif"),

            WalkUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong up", @"demon_strong\walk_strong_up.gif"),
            WalkDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong down", @"demon_strong\walk_strong_down.gif"),
            WalkLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong left", @"demon_strong\walk_strong_left.gif"),
            WalkRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "strong right", @"demon_strong\walk_strong_right.gif")
        };

        private SpriteSet LoadDemonArcherSprites() => new SpriteSet
        {
            IdleDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer idle down", @"demon_archer\walk_archer_down.gif"),
            IdleUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer idle up", @"demon_archer\walk_archer_up.gif"),
            IdleLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer idle left", @"demon_archer\walk_archer_left.gif"),
            IdleRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer idle right", @"demon_archer\walk_archer_right.gif"),

            WalkUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer up", @"demon_archer\walk_archer_up.gif"),
            WalkDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer down", @"demon_archer\walk_archer_down.gif"),
            WalkLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer left", @"demon_archer\walk_archer_left.gif"),
            WalkRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer right", @"demon_archer\walk_archer_right.gif"),

            AttackDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer atk down", @"demon_archer\shoot_archer_down.gif"),
            AttackUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer atk up", @"demon_archer\shoot_archer_up.gif"),
            AttackLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer atk left", @"demon_archer\shoot_archer_left.gif"),
            AttackRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "archer atk right", @"demon_archer\shoot_archer_right.gif"),
        };

        private SpriteSet LoadDemonMageSprites() => new SpriteSet
        {
            IdleDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage idle down", @"demon_mage\walk_down_mage.gif"),
            IdleUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage idle up", @"demon_mage\walk_up_mage.gif"),
            IdleLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage idle left", @"demon_mage\walk_left_mage.gif"),
            IdleRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage idle right", @"demon_mage\walk_right_mage.gif"),

            WalkUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage up", @"demon_mage\walk_up_mage.gif"),
            WalkDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage down", @"demon_mage\walk_down_mage.gif"),
            WalkLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage left", @"demon_mage\walk_left_mage.gif"),
            WalkRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage right", @"demon_mage\walk_right_mage.gif"),

            AttackDown = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage cast down", @"demon_mage\spellcast_down.gif"),
            AttackUp = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage cast up", @"demon_mage\spellcast_up.gif"),
            AttackLeft = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage cast left", @"demon_mage\spellcast_left.gif"),
            AttackRight = LoadAnim(new Size(EnemySizePx * SpriteScale, EnemySizePx * SpriteScale), "mage cast right", @"demon_mage\spellcast_right.gif"),
        };

        private static Anim GetAnim(SpriteSet s, Facing face, bool moving, bool attacking)
        {
            if (attacking)
            {
                return face switch
                {
                    Facing.Down => SpriteSet.FirstNonNull(s.AttackDown, s.IdleDown, s.WalkDown),
                    Facing.Up => SpriteSet.FirstNonNull(s.AttackUp, s.IdleUp, s.WalkUp),
                    Facing.Left => SpriteSet.FirstNonNull(s.AttackLeft, s.IdleLeft, s.WalkLeft),
                    Facing.Right => SpriteSet.FirstNonNull(s.AttackRight, s.IdleRight, s.WalkRight),
                    _ => SpriteSet.FirstNonNull(s.AttackDown, s.IdleDown, s.WalkDown)
                };
            }

            if (moving)
            {
                return face switch
                {
                    Facing.Down => SpriteSet.FirstNonNull(s.WalkDown, s.IdleDown),
                    Facing.Up => SpriteSet.FirstNonNull(s.WalkUp, s.IdleUp),
                    Facing.Left => SpriteSet.FirstNonNull(s.WalkLeft, s.IdleLeft),
                    Facing.Right => SpriteSet.FirstNonNull(s.WalkRight, s.IdleRight),
                    _ => s.WalkDown
                };
            }
            else
            {
                return face switch
                {
                    Facing.Down => SpriteSet.FirstNonNull(s.IdleDown, s.WalkDown),
                    Facing.Up => SpriteSet.FirstNonNull(s.IdleUp, s.WalkUp),
                    Facing.Left => SpriteSet.FirstNonNull(s.IdleLeft, s.WalkLeft),
                    Facing.Right => SpriteSet.FirstNonNull(s.IdleRight, s.WalkRight),
                    _ => s.IdleDown
                };
            }
        }

        private static Anim GetAnim(SpriteSet s, Facing face, bool moving)
            => GetAnim(s, face, moving, attacking: false);

        private void UpdateAnimations(int dt)
        {
            var pa = GetAnim(player.Sprites, player.Facing, player.Moving, attacking: false);
            pa?.Update(dt);

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                bool attacking = ((e.Kind == EnemyKind.Archer) || (e.Kind == EnemyKind.Mage)) && e.IsCasting;
                var ea = GetAnim(e.Sprites, e.Facing, e.Moving, attacking);
                ea?.Update(dt);
            }
        }
    }
}