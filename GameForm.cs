using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinTimer = System.Windows.Forms.Timer;
using Image = System.Drawing.Image;
using Font = System.Drawing.Font;

namespace Rogalik
{
    public partial class GameForm : Form
    {
        private float xpFillShown = 0f;
        private const int TargetFps = 30;
        private const int PlayerSizePx = 48;
        private const int EnemySizePx = 40;
        private const int SpriteScale = 2;
        private const int MinFrameMs = 33;
        private const int MaxUniqueAbilities = 6;
        private const int MaxStackLevel = 10;
        private readonly WinTimer gameTimer;
        private readonly WinTimer enemySpawnTimer = new WinTimer { Interval = 1600 };
        private readonly Random rng = new Random();

        private bool keyW, keyA, keyS, keyD;

        private YellowHealthBar hpBar;
        private Panel overlayGameOver;
        private Panel gameOverBox;
        private MenuButton overBtnRestart, overBtnMenu;

        private AbilityPicker picker;

        private Player player;
        private readonly System.Collections.Generic.List<Enemy> enemies = new();
        private readonly System.Collections.Generic.List<Bullet> bullets = new();
        private readonly System.Collections.Generic.List<EffectBase> effects = new();

        private SpriteSet angelSprites, demonSprites;

        private int level = 1;
        private int xp = 0;
        private int xpToNext = 30;

        private double shootAccumMs = 0;

        private bool isFullscreen = false;
        private Rectangle windowedBounds;
        private FormBorderStyle windowedBorder;
        private FormWindowState windowedState;

        private bool isPaused = false;

        private readonly System.Collections.Generic.Dictionary<string, int> abilityLevels = new();
        private readonly System.Collections.Generic.List<string> abilityOrder = new();
        private readonly System.Collections.Generic.Dictionary<string, int> cdLeftMs = new();
        private AbilityDef GetDef(string id) => AbilityCatalog.All.FirstOrDefault(a => a.Id == id);

        private Image gameBg;

        private volatile bool assetsReady = false;
        private bool introRunning = false;

        public GameForm()
        {
            Text = "Rogalik — игра";
            ClientSize = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;

            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            KeyPreview = true;
            BackColor = Color.FromArgb(24, 24, 28);

            BuildUI();
            CreatePlayer();

            gameBg = TryLoadGameBg();
            Resize += (s, e) => Invalidate();

            StartPreload();
            picker = new AbilityPicker(this);

            gameTimer = new WinTimer { Interval = Math.Max(1, 1000 / TargetFps) };
            gameTimer.Tick += GameLoop;

            enemySpawnTimer.Tick += (s, e) =>
            {
                if (!assetsReady || introRunning) return;
                SpawnAnyEnemyRandom();
            };

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            FormClosed += (s, e) =>
            {
                Save.HighScore = Math.Max(Save.HighScore, killCount);
                try { Cursor.Show(); Cursor.Show(); } catch { }
                cursorHidden = false;
            };

            EnterFullscreen();
            HideCursorIfFullscreen();

            // Всегда показываем диалог при старте
            BeginIntroDialogue();
        }

        private void StartPreload()
        {
            Task.Run(() =>
            {
                try
                {
                    demonSprites ??= LoadDemonCommonSprites();
                    demonStrongSprites ??= LoadDemonStrongSprites();
                    demonArcherSprites ??= LoadDemonArcherSprites();
                    demonMageSprites ??= LoadDemonMageSprites();
                    EnsureArcherArrowImages();
                }
                catch { }
                finally
                {
                    assetsReady = true;
                }
            });
        }

        private void UpdateXpFill(int dt)
        {
            float target = (xpToNext <= 0) ? 1f : Math.Max(0f, Math.Min(1f, (float)xp / xpToNext));
            float k = Math.Min(1f, dt / 180f);
            xpFillShown += (target - xpFillShown) * k;
        }

        private DateTime lastTick = DateTime.UtcNow;
        private void GameLoop(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            if (introRunning)
            {
                lastTick = now;
                return;
            }

            if (isPaused)
            {
                lastTick = now;
                return;
            }

            var dt = (int)(now - lastTick).TotalMilliseconds;
            if (dt <= 0) dt = 1;
            lastTick = now;

            UpdatePlayer(dt);
            if (introRunning) return;
            UpdateEnemies(dt);

            shootAccumMs += dt;
            if (shootAccumMs >= player.AttackCooldownMs)
            {
                shootAccumMs = 0;
                PlayerAutoShoot();
            }

            UpdateBullets(dt);
            UpdateActiveAbilities(dt);
            UpdateEffects(dt);
            UpdateAnimations(dt);
            UpdateXpFill(dt);

            Invalidate();
        }

        private void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            enemySpawnTimer.Stop();
            gameTimer.Stop();
        }

        private void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            lastTick = DateTime.UtcNow;
            gameTimer.Start();
            enemySpawnTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode.SourceOver;

            if (gameBg != null)
            {
                var dest = GetCoverRect(ClientSize, gameBg.Size);
                g.DrawImage(gameBg, dest);
            }

            foreach (var en in enemies)
            {
                bool attacking = ((en.Kind == EnemyKind.Archer) || (en.Kind == EnemyKind.Mage)) && en.IsCasting;
                var anim = GetAnim(en.Sprites, en.Facing, en.Moving, attacking);
                var frame = anim.Cur;
                if (frame != null)
                    g.DrawImage(frame, new RectangleF(en.X, en.Y, en.W, en.H));
            }

            DrawEnemyHpBars(g);

            foreach (var ef in effects)
                ef.Draw(g);

            var pAnim = GetAnim(player.Sprites, player.Facing, player.Moving, attacking: false);
            var pFrame = pAnim.Cur;
            if (pFrame != null)
                g.DrawImage(pFrame, new RectangleF(player.X, player.Y, player.W, player.H));

            DrawEnemyProjectiles(g);

            foreach (var b in bullets)
            {
                float glowR = b.Radius * 2.6f;
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(b.X - glowR, b.Y - glowR, glowR * 2, glowR * 2);
                    using var pgb = new PathGradientBrush(gp)
                    {
                        CenterColor = Color.FromArgb(140, 255, 240, 160),
                        SurroundColors = new[] { Color.FromArgb(0, 255, 240, 160) }
                    };
                    g.FillPath(pgb, gp);
                }

                using var brInner = new SolidBrush(Color.FromArgb(230, 255, 215, 0));
                g.FillEllipse(brInner, b.X - b.Radius, b.Y - b.Radius, b.Radius * 2, b.Radius * 2);

                using var pen = new Pen(Color.FromArgb(220, 255, 235, 120), 1.5f);
                g.DrawEllipse(pen, b.X - b.Radius, b.Y - b.Radius, b.Radius * 2, b.Radius * 2);
            }

            DrawXpBar(g);
            DrawKillCounter(g);
            DrawAbilitySlots(g);
        }

        private void DrawEnemyProjectiles(Graphics g)
        {
            if (enemyProjectiles == null || enemyProjectiles.Count == 0) return;

            foreach (var p in enemyProjectiles)
            {
                using (var gp = new GraphicsPath())
                {
                    float glow = Math.Max(10, p.Radius * 3.2f);
                    gp.AddEllipse(p.X - glow, p.Y - glow, glow * 2, glow * 2);
                    using var glowBr = new PathGradientBrush(gp)
                    {
                        CenterColor = Color.FromArgb(70, 255, 200, 140),
                        SurroundColors = new[] { Color.FromArgb(0, 255, 200, 140) }
                    };
                    g.FillPath(glowBr, gp);
                }

                if (p.Kind == EnemyProjectileKind.MageOrb)
                {
                    using (var gp = new GraphicsPath())
                    {
                        float glow = Math.Max(18, p.Radius * 3.4f);
                        gp.AddEllipse(p.X - glow, p.Y - glow, glow * 2, glow * 2);
                        using var glowBr = new PathGradientBrush(gp)
                        {
                            CenterColor = Color.FromArgb(110, 80, 60, 130),
                            SurroundColors = new[] { Color.FromArgb(0, 80, 60, 130) }
                        };
                        g.FillPath(glowBr, gp);
                    }

                    using (var core = new SolidBrush(Color.FromArgb(235, 8, 8, 10)))
                        g.FillEllipse(core, p.X - p.Radius, p.Y - p.Radius, p.Radius * 2, p.Radius * 2);

                    using var ring = new Pen(Color.FromArgb(200, 170, 160, 220), 1.6f);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawEllipse(ring, p.X - p.Radius, p.Y - p.Radius, p.Radius * 2, p.Radius * 2);

                    float tail = Math.Max(10, p.Radius * 2.5f);
                    using var tailPen = new Pen(Color.FromArgb(120, 120, 110, 180), 2f);
                    g.DrawLine(tailPen, p.X, p.Y, p.X - p.VX * tail, p.Y - p.VY * tail);

                    continue;
                }

                if (p.Image != null)
                {
                    var state = g.Save();
                    try
                    {
                        g.TranslateTransform(p.X, p.Y);
                        g.RotateTransform(p.AngleRad * 180f / (float)Math.PI);

                        int drawW = Math.Max(22, (int)(p.Radius * 6f));
                        int drawH = Math.Max(6, (int)(p.Radius * 2.2f));
                        var r = new Rectangle(-drawW / 2, -drawH / 2, drawW, drawH);
                        g.DrawImage(p.Image, r);
                    }
                    finally
                    {
                        g.Restore(state);
                    }
                }
                else
                {
                    var state = g.Save();
                    try
                    {
                        g.TranslateTransform(p.X, p.Y);
                        g.RotateTransform(p.AngleRad * 180f / (float)Math.PI);

                        using var body = new SolidBrush(Color.FromArgb(230, 200, 90));
                        using var edge = new Pen(Color.FromArgb(255, 230, 140), 1.5f);

                        int w = Math.Max(18, (int)(p.Radius * 6f));
                        int h = Math.Max(4, (int)(p.Radius * 2f));

                        g.FillRectangle(body, -w / 2, -h / 2, w, h);
                        g.DrawRectangle(edge, -w / 2, -h / 2, w, h);

                        PointF a = new PointF(w / 2f + 6, 0);
                        PointF b = new PointF(w / 2f - 2, -h);
                        PointF c = new PointF(w / 2f - 2, h);
                        using var brHead = new SolidBrush(Color.FromArgb(255, 240, 160));
                        g.FillPolygon(brHead, new[] { a, b, c });
                        g.DrawPolygon(edge, new[] { a, b, c });
                    }
                    finally
                    {
                        g.Restore(state);
                    }
                }
            }
        }

        private static System.Drawing.Bitmap LoadImageUnlockedRel(string rel)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] c =
                {
            System.IO.Path.Combine(baseDir, rel),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..", rel)),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..", rel)),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\..", rel)),
        };
                foreach (var p in c)
                {
                    if (System.IO.File.Exists(p))
                    {
                        using var fs = new System.IO.FileStream(p, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                        using var tmp = Image.FromStream(fs);
                        return new Bitmap(tmp);
                    }
                }
            }
            catch { }
            return null;
        }

        private static Bitmap CropTransparent(Bitmap src, byte alphaThreshold = 8)
        {
            try
            {
                var rect = new Rectangle(0, 0, src.Width, src.Height);
                var data = src.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    int w = src.Width, h = src.Height, stride = Math.Abs(data.Stride);
                    var buf = new byte[stride * h];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);

                    int left = w, right = -1, top = h, bottom = -1;
                    for (int y = 0; y < h; y++)
                    {
                        int row = y * stride;
                        for (int x = 0; x < w; x++)
                        {
                            int idx = row + x * 4;
                            byte a = buf[idx + 3];
                            if (a > alphaThreshold)
                            {
                                if (x < left) left = x;
                                if (x > right) right = x;
                                if (y < top) top = y;
                                if (y > bottom) bottom = y;
                            }
                        }
                    }

                    if (right >= left && bottom >= top)
                    {
                        var crop = new Rectangle(left, top, right - left + 1, bottom - top + 1);
                        return src.Clone(crop, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    }
                    return (Bitmap)src.Clone();
                }
                finally { src.UnlockBits(data); }
            }
            catch { return (Bitmap)src.Clone(); }
        }

        private class DialogueOverlay : Control
        {
            private readonly Image angel, demon;
            private readonly (bool isAngel, string text)[] lines;
            private int index = 0;

            private string shown = "";
            private int charPos = 0;
            private int msAccum = 0;
            private const int MsPerChar = 25;
            private readonly WinTimer t = new WinTimer { Interval = 16 };

            public event Action Finished;

            public DialogueOverlay(Image angelImg, Image demonImg, (bool, string)[] lines)
            {
                this.angel = angelImg;
                this.demon = demonImg;
                this.lines = lines;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.SupportsTransparentBackColor, true);

                BackColor = Color.Transparent;
                Dock = DockStyle.Fill;

                t.Tick += (_, __) =>
                {
                    if (index >= lines.Length) return;
                    msAccum += t.Interval;
                    while (msAccum >= MsPerChar && charPos < lines[index].Item2.Length)
                    {
                        msAccum -= MsPerChar;
                        charPos++;
                        shown = lines[index].Item2.Substring(0, charPos);
                        Invalidate();
                    }
                };
                t.Start();

                MouseDown += (_, __) =>
                {
                    if (index >= lines.Length) return;

                    if (charPos < lines[index].Item2.Length)
                    {
                        charPos = lines[index].Item2.Length;
                        shown = lines[index].Item2;
                        Invalidate();
                    }
                    else
                    {
                        index++;
                        if (index >= lines.Length)
                        {
                            t.Stop();
                            Finished?.Invoke();
                            Parent?.Controls.Remove(this);
                            Dispose();
                        }
                        else
                        {
                            charPos = 0;
                            shown = "";
                            msAccum = 0;
                            Invalidate();
                        }
                    }
                };
            }
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using var shade = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
                e.Graphics.FillRectangle(shade, ClientRectangle);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                bool angelTurn = (index < lines.Length) ? lines[index].Item1 : false;

                float margin = 20f;
                float baseH = Math.Min(Height * 0.5f, 380f);
                float baseW = baseH * 0.8f;

                float scaleActive = 1.10f;
                float scalePassive = 0.90f;

                void DrawChar(Image img, bool active, bool left)
                {
                    if (img == null) return;
                    float s = (active ? scaleActive : scalePassive);
                    int w = (int)(baseW * s);
                    int h = (int)(baseH * s);
                    int x = left ? (int)margin : (Width - (int)margin - w);
                    int y = Height - h - (int)margin - 120;

                    if (!active)
                    {
                        using var ia = new System.Drawing.Imaging.ImageAttributes();
                        var m = new System.Drawing.Imaging.ColorMatrix(
                            new float[][]
                            {
                        new float[]{0.65f,0,0,0,0},
                        new float[]{0,0.65f,0,0,0},
                        new float[]{0,0,0.65f,0,0},
                        new float[]{0,0,0,1f,0},
                        new float[]{0,0,0,0,1}
                            });
                        ia.SetColorMatrix(m);
                        g.DrawImage(
                            img,
                            new Rectangle(x, y, w, h),
                            0, 0, img.Width, img.Height,
                            GraphicsUnit.Pixel,
                            ia
                        );
                    }
                    else
                    {
                        g.DrawImage(img, new Rectangle(x, y, w, h));
                    }
                }

                DrawChar(angel, angelTurn, left: true);
                DrawChar(demon, !angelTurn, left: false);

                float pad = 20f;
                float boxH = 120f;
                var box = new RectangleF(pad, Height - boxH - pad, Width - pad * 2f, boxH);

                using (var path = RoundRect(box, 12f))
                {
                    using var bg = new SolidBrush(Color.FromArgb(190, 20, 20, 24));
                    g.FillPath(bg, path);
                    using var pen = new Pen(Color.FromArgb(200, 90, 98), 2f);
                    g.DrawPath(pen, path);
                }

                string speaker = angelTurn ? "Аранбо" : "Ардесат";

                var nameRect = new RectangleF(box.X + 14, box.Y + 10, 200, 24);
                using (var fName = new Font("Segoe UI Semibold", 12f))
                using (var brName = new SolidBrush(Color.FromArgb(240, 220, 160)))
                {
                    g.DrawString(speaker, fName, brName, nameRect);
                }

                var textRect = new RectangleF(box.X + 14, nameRect.Bottom + 4, box.Width - 28, box.Height - (nameRect.Bottom - box.Y) - 14);
                using (var fText = new Font("Segoe UI", 14f))
                using (var brText = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        Trimming = StringTrimming.Word
                    };
                    g.DrawString(shown, fText, brText, textRect, sf);
                }

                string hint = "ЛКМ — далее";
                using (var fHint = new Font("Segoe UI", 10f, FontStyle.Italic))
                using (var brHint = new SolidBrush(Color.FromArgb(220, 200, 200, 200)))
                {
                    var sz = g.MeasureString(hint, fHint);
                    g.DrawString(hint, fHint, brHint, box.Right - sz.Width - 10, box.Bottom - sz.Height - 6);
                }
            }

            private static GraphicsPath RoundRect(RectangleF r, float radius)
            {
                float d = radius * 2f;
                var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
                path.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
                path.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private void BeginIntroDialogue()
        {
            introRunning = true;
            if (gameTimer.Enabled) gameTimer.Stop();
            if (enemySpawnTimer.Enabled) enemySpawnTimer.Stop();
            isPaused = true;

            ShowCursor();
            ResetMovementKeys();
            if (player != null) player.Moving = false;

            Bitmap angel = LoadImageUnlockedRel(@"StoryCharacters\Angel_full.png");
            Bitmap demon = LoadImageUnlockedRel(@"StoryCharacters\Demon_full.png");
            if (angel != null) angel = CropTransparent(angel);
            if (demon != null) demon = CropTransparent(demon);
            angel ??= LoadImageUnlockedRel(@"Assets\angel.png");
            demon ??= LoadImageUnlockedRel(@"Assets\demon.png");

            var lines = Save.SessionIntroShown
                ? new (bool isAngel, string text)[]
                {
            (true,  "Я вернулся!"),
            (false, "Атакуйте его!")
                }
                : new (bool isAngel, string text)[]
                {
            (true,  "О боже, Ардесат, как ты мог встать на сторону Сатаны? Я разочарован в тебе, ты был хорошим командиром в отряде Бога."),
            (false, "Слабый и глупый прихвостень бога, я стал сильнее и умнее всех вас, у меня есть всё: Сила, Ум, Армия. А ты и дальше прислуживай своему Богу."),
            (true,  "Мне придётся уничтожить тебя, ты был мудрым ангелом который стал глупым и гордым демоном."),
            (false, "Тогда пусть решит бой. Посмотрим, сколько ты выдержишь. Вперёд демоны! Уничтожьте его!"),
            (true,  "По слову Бога, уничтожу твоих демонов, а потом и тебя!")
                };

            var overlay = new DialogueOverlay(angel, demon, lines);
            overlay.Finished += () =>
            {
                Save.IntroSeen = true;
                Save.SessionIntroShown = true;

                introRunning = false;
                isPaused = false;
                lastTick = DateTime.UtcNow;

                gameTimer.Start();
                enemySpawnTimer.Start();

                HideCursorIfFullscreen();
                Focus();
                Select();
            };

            Controls.Add(overlay);
            overlay.BringToFront();
        }

        private static Image LoadNoLock(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var img = Image.FromStream(fs);
            return new Bitmap(img);
        }

        private static string FindDirUpwards(string dirName, int maxLevels = 6)
        {
            var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < maxLevels && d != null; i++, d = d.Parent)
            {
                string p = Path.Combine(d.FullName, dirName);
                if (Directory.Exists(p)) return p;
            }
            return null;
        }

        private Image TryLoadGameBg()
        {
            try
            {
                string fonDir = FindDirUpwards("fon");
                if (fonDir == null) return null;

                foreach (var name in new[] { "Fon_Game.jpg", "Fon_Game.png", "Fon_Game.jpeg", "Fon_Game.bmp" })
                {
                    string p = Path.Combine(fonDir, name);
                    if (File.Exists(p)) return LoadNoLock(p);
                }

                var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                var alt = Directory.GetFiles(fonDir, "Fon_Game.*", SearchOption.TopDirectoryOnly)
                                   .FirstOrDefault(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));
                if (alt != null) return LoadNoLock(alt);

                string fallback = Path.Combine(fonDir, "Armagedon.jpg");
                if (File.Exists(fallback)) return LoadNoLock(fallback);

                var any = Directory.GetFiles(fonDir, "*.*", SearchOption.TopDirectoryOnly)
                                   .FirstOrDefault(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));
                if (any != null) return LoadNoLock(any);
            }
            catch { }
            return null;
        }

        private static Rectangle GetCoverRect(Size container, Size content)
        {
            if (content.Width <= 0 || content.Height <= 0)
                return new Rectangle(0, 0, container.Width, container.Height);

            float scale = Math.Max(container.Width / (float)content.Width,
                                   container.Height / (float)content.Height);
            int w = (int)Math.Ceiling(content.Width * scale);
            int h = (int)Math.Ceiling(content.Height * scale);
            int x = (container.Width - w) / 2;
            int y = (container.Height - h) / 2;
            return new Rectangle(x, y, w, h);
        }

        private static RectangleF Rect(Player p) => new RectangleF(p.X, p.Y, p.W, p.H);
        private static RectangleF Rect(Enemy e) => new RectangleF(e.X, e.Y, e.W, e.H);

        private Enemy NearestEnemy(float x, float y, Func<Enemy, bool> pred = null, Enemy except = null)
        {
            Enemy best = null;
            double bestd = double.MaxValue;
            foreach (var e in enemies)
            {
                if (e == except) continue;
                if (pred != null && !pred(e)) continue;
                float cx = e.X + e.W / 2f, cy = e.Y + e.H / 2f;
                double d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d2 < bestd) { bestd = d2; best = e; }
            }
            return best;
        }
    }
}