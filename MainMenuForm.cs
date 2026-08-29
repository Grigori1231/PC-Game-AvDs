using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WinTimer = System.Windows.Forms.Timer;

namespace Rogalik
{
    public class MainMenuForm : Form
    {
        private PictureBox bg;
        private Panel shade;
        private Panel center;
        private LightDarkTitle title;
        private readonly List<MenuButton> menuButtons = new List<MenuButton>();
        private Label lblRecord;
        private Button btnResetRecord;
        private Panel resetOverlay;
        private Panel resetBox;
        private Label resetTitle, resetText;
        private MenuButton resetYes, resetNo;

        public MainMenuForm()
        {
            Text = "Rogalik — меню";
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;
            KeyPreview = true;

            BuildUI();
            Resize += (s, e) => LayoutCenter();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        private void TransitionOpen(Func<Form> create, string caption, int ms = 1000)
        {
            Hide();
            LoadingForm.ShowFullscreenFor(this, caption, ms);
            using (var f = create())
                f.ShowDialog(this);
            Show();
        }

        private void BuildUI()
        {
            Activated += (s, e) =>
            {
                if (lblRecord != null)
                {
                    lblRecord.Text = $"Рекорд: {Save.HighScore}";
                }
                LayoutCenter();
                try { Cursor.Show(); Cursor.Show(); } catch { }
            };

            BackgroundImage = TryLoadMenuBg();
            BackgroundImageLayout = ImageLayout.Stretch;

            shade = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            Controls.Add(shade);

            lblRecord = new Label
            {
                AutoSize = true,
                Text = $"Рекорд: {Save.HighScore}",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 14f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(lblRecord);
            lblRecord.BringToFront();

            btnResetRecord = new Button
            {
                Text = "Сбросить рекорд",
                AutoSize = true,
                Width = 170,
                Height = 30,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnResetRecord.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 92);
            btnResetRecord.FlatAppearance.BorderSize = 1;
            Controls.Add(btnResetRecord);
            btnResetRecord.BringToFront();
            btnResetRecord.Click += (s, e) => ShowResetOverlay();

            BuildResetOverlay();

            center = new Panel
            {
                Width = 560,
                Height = 520,
                BackColor = Color.Transparent
            };
            shade.Controls.Add(center);

            title = new LightDarkTitle
            {
                Text = "AvDs",
                Width = center.Width,
                Height = 120,
                Left = 0,
                Top = 0
            };
            center.Controls.Add(title);

            int btnW = 420, btnH = 60, spacing = 18;
            int top = title.Bottom + 12;

            center.Controls.Add(MakeButton("Играть", top, btnW, btnH, (s, e) =>
            {
                Hide();
                LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
                using (var g = new GameForm())
                    g.ShowDialog(this);
                Show();
            }));
            top += btnH + spacing;

            center.Controls.Add(MakeButton("Типы врагов", top, btnW, btnH, (s, e) =>
            {
                TransitionOpen(() => new EnemyTypesForm(), "Загрузка...", 3000);
            }));
            top += btnH + spacing;

            center.Controls.Add(MakeButton("Сюжет", top, btnW, btnH, (s, e) =>
            {
                LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
                ShowStory();
            }));
            top += btnH + spacing;

            center.Controls.Add(MakeButton("Выход", top, btnW, btnH, (s, e) => Close()));

            LayoutCenter();
        }

        private void LayoutCenter()
        {
            center.Left = (ClientSize.Width - center.Width) / 2;
            center.Top = (ClientSize.Height - center.Height) / 2;

            if (lblRecord != null)
            {
                lblRecord.Left = ClientSize.Width - lblRecord.Width - 16;
                lblRecord.Top = 10;
            }

            if (btnResetRecord != null)
            {
                btnResetRecord.Left = ClientSize.Width - btnResetRecord.Width - 16;
                btnResetRecord.Top = (lblRecord?.Bottom ?? 10) + 8;
            }
        }

        private Control MakeButton(string text, int top, int width, int height, EventHandler onClick)
        {
            var b = new MenuButton
            {
                Text = text,
                Width = width,
                Height = height,
                Left = (center.Width - width) / 2,
                Top = top
            };
            b.Click += onClick;
            menuButtons.Add(b);
            return b;
        }

        private void ShowStory()
        {
            Hide();
            using (var f = new StoryForm())
                f.ShowDialog(this);
            Show();
        }

        private void BuildResetOverlay()
        {
            resetOverlay = new Panel
            {
                Visible = false,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(140, 0, 0, 0)
            };
            Controls.Add(resetOverlay);
            resetOverlay.BringToFront();

            resetBox = new Panel
            {
                Width = 420,
                Height = 200,
                BackColor = Color.FromArgb(36, 36, 44),
                BorderStyle = BorderStyle.FixedSingle
            };
            resetOverlay.Controls.Add(resetBox);

            resetTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Text = "Подтверждение",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            resetBox.Controls.Add(resetTitle);

            resetText = new Label
            {
                Dock = DockStyle.Top,
                Height = 54,
                Text = "Сбросить рекорд?",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 12, FontStyle.Regular)
            };
            resetBox.Controls.Add(resetText);

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 86,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(20, 12, 20, 20),
                BackColor = Color.Transparent
            };
            resetBox.Controls.Add(panelButtons);

            resetYes = new MenuButton
            {
                Text = "Сбросить",
                Width = 160,
                Height = 46,
                Font = new Font("Segoe UI Semibold", 12f),
                Margin = new Padding(10, 8, 10, 8)
            };
            resetNo = new MenuButton
            {
                Text = "Отмена",
                Width = 160,
                Height = 46,
                Font = new Font("Segoe UI Semibold", 12f),
                Margin = new Padding(10, 8, 10, 8)
            };

            resetYes.Click += (s, e) =>
            {
                Save.ResetHighScore();
                lblRecord.Text = $"Рекорд: {Save.HighScore}";
                HideResetOverlay();
                LayoutCenter();
            };
            resetNo.Click += (s, e) => HideResetOverlay();

            panelButtons.Controls.Add(resetYes);
            panelButtons.Controls.Add(resetNo);

            resetOverlay.VisibleChanged += (s, e) => { if (resetOverlay.Visible) CenterResetBox(); };
            resetOverlay.Resize += (s, e) => { if (resetOverlay.Visible) CenterResetBox(); };
        }

        private void ShowResetOverlay()
        {
            resetOverlay.Visible = true;
            resetOverlay.BringToFront();
            CenterResetBox();
        }

        private void HideResetOverlay()
        {
            resetOverlay.Visible = false;
        }

        private void CenterResetBox()
        {
            if (resetBox == null) return;
            resetBox.Left = (ClientSize.Width - resetBox.Width) / 2;
            resetBox.Top = (ClientSize.Height - resetBox.Height) / 2;
        }

        private static Image TryLoadMenuBg()
        {
            static bool IsImg(string path)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
            }

            static Image LoadNoLock(string path)
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var tmp = Image.FromStream(fs);
                return new Bitmap(tmp);
            }

            var log = new StringBuilder();
            void L(string s) { log.AppendLine(s); }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                L("BaseDir: " + baseDir);

                var d = new DirectoryInfo(baseDir);
                for (int level = 0; level < 10 && d != null; level++, d = d.Parent)
                {
                    string fonDir = Path.Combine(d.FullName, "fon");
                    L($"Check dir: {fonDir}");
                    if (!Directory.Exists(fonDir)) continue;

                    string[] names = { "Armagedon", "Armageddon" };
                    string[] exts = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                    foreach (var n in names)
                    {
                        foreach (var ext in exts)
                        {
                            string p = Path.Combine(fonDir, n + ext);
                            L("Try exact: " + p);
                            if (File.Exists(p))
                            {
                                L("FOUND exact: " + p);
                                System.Diagnostics.Debug.WriteLine(log.ToString());
                                return LoadNoLock(p);
                            }
                        }
                    }

                    foreach (var f in Directory.GetFiles(fonDir, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        if (IsImg(f))
                        {
                            L("FOUND first image: " + f);
                            System.Diagnostics.Debug.WriteLine(log.ToString());
                            return LoadNoLock(f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BG ERROR: " + ex);
            }

            try
            {
                string baseDir2 = AppDomain.CurrentDomain.BaseDirectory;
                string[] c =
                {
                Path.Combine(baseDir2, "Assets", "menu_bg.jpg"),
                Path.GetFullPath(Path.Combine(baseDir2, @"..\..\Assets\menu_bg.jpg")),
                Path.GetFullPath(Path.Combine(baseDir2, @"..\..\..\Assets\menu_bg.jpg")),
                Path.GetFullPath(Path.Combine(baseDir2, @"..\..\..\..\Assets\menu_bg.jpg")),
            };
                foreach (var p in c)
                    if (File.Exists(p)) return Image.FromFile(p);
            }
            catch { }

            System.Diagnostics.Debug.WriteLine("BG NOT FOUND. Using placeholder.");

            var bmp = new Bitmap(64, 64);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(140, 140, 140));
            return bmp;
        }
    }

    public class LightDarkTitle : Control
    {
        public LightDarkTitle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 120;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            var ff = new FontFamily("Segoe UI");
            float baseEm = 48f;
            var fmt = StringFormat.GenericTypographic;
            fmt.Alignment = StringAlignment.Near;
            fmt.LineAlignment = StringAlignment.Near;

            path.AddString(Text, ff, (int)FontStyle.Bold, baseEm, new Point(0, 0), fmt);

            var bounds = path.GetBounds();
            float margin = 8f;
            float availW = Math.Max(1f, Width - margin * 2f);
            float availH = Math.Max(1f, Height - margin * 2f);
            float sx = availW / bounds.Width;
            float sy = availH / bounds.Height;
            float scale = Math.Min(sx, sy);

            g.TranslateTransform(
                margin + (availW - bounds.Width * scale) / 2f - bounds.Left * scale,
                margin + (availH - bounds.Height * scale) / 2f - bounds.Top * scale);
            g.ScaleTransform(scale, scale);

            using var textGradient = new LinearGradientBrush(
                new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height),
                Color.White, Color.Black, 0f);
            g.FillPath(textGradient, path);

            using var shadowPen = new Pen(Color.FromArgb(150, 0, 0, 0), 8);
            g.DrawPath(shadowPen, path);
            using var innerPen = new Pen(Color.FromArgb(90, 255, 255, 255), 2);
            g.DrawPath(innerPen, path);
        }
    }

    public class MenuButton : Control
    {
        private readonly WinTimer anim = new WinTimer();
        private bool hovered;
        private float scale = 1.0f;
        private float targetScale = 1.0f;
        private float hoverPhase;

        public Color AccentColor { get; set; } = Color.FromArgb(90, 200, 170);
        public Color BaseColor1 { get; set; } = Color.FromArgb(36, 36, 44);
        public Color BaseColor2 { get; set; } = Color.FromArgb(28, 28, 36);
        public Color BorderColor { get; set; } = Color.FromArgb(80, 80, 92);

        public MenuButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 16f);
            Size = new Size(360, 60);

            anim.Interval = 16;
            anim.Tick += (s, e) => TickAnim();
            anim.Start();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            using var p = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 12);
            Region?.Dispose();
            Region = new Region(p);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            targetScale = 0.96f;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            targetScale = 1.0f;
            base.OnMouseLeave(e);
        }

        private void TickAnim()
        {
            scale = Lerp(scale, targetScale, 0.2f);
            if (hovered) hoverPhase += 0.01f; else hoverPhase = 0f;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            g.TranslateTransform(Width / 2f, Height / 2f);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-Width / 2f, -Height / 2f);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 12;

            var dark1 = BaseColor1;
            var dark2 = BaseColor2;
            var accent = AccentColor;
            var border = BorderColor;

            float breath = hovered ? (0.15f * (0.5f + 0.5f * (float)Math.Sin(hoverPhase * 2 * Math.PI))) : 0f;
            float brighten = (hovered ? 0.08f : 0f) + breath;

            Color Bright(Color c, float k)
            {
                int r = Math.Min(255, (int)(c.R + 255 * k));
                int g2 = Math.Min(255, (int)(c.G + 255 * k));
                int b = Math.Min(255, (int)(c.B + 255 * k));
                return Color.FromArgb(c.A, r, g2, b);
            }

            var c1 = Bright(dark1, brighten);
            var c2 = Bright(dark2, brighten / 2f);

            using (var fill = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical))
            using (var path = RoundRect(rect, radius))
            {
                g.FillPath(fill, path);

                using var p = new Pen(border, 1f);
                g.DrawPath(p, path);

                var glow = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height / 2);
                using var glowBrush = new LinearGradientBrush(glow,
                    Color.FromArgb(hovered ? 60 : 40, accent), Color.FromArgb(0, accent), LinearGradientMode.Vertical);
                g.FillPath(glowBrush, RoundRect(glow, radius - 2));
            }

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(Color.White);
            g.DrawString(Text, Font, textBrush, rect, sf);
        }

        protected override void OnClick(EventArgs e)
        {
            targetScale = 0.93f;
            var t = new WinTimer { Interval = 80 };
            t.Tick += (s, ev) =>
            {
                t.Stop();
                t.Dispose();
                targetScale = hovered ? 0.96f : 1.0f;
            };
            t.Start();
            base.OnClick(e);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}