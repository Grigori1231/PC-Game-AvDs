using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinTimer = System.Windows.Forms.Timer;

namespace Rogalik
{
    public class LoadingForm : Form
    {
        private readonly Func<Task> work;
        private readonly int minMs;
        private readonly string caption;

        private Image bg;
        private Panel shade;
        private RuneLoader rune;
        private Label lbl;

        public LoadingForm(string text, Func<Task> work, int minVisibleMs = 3000)
        {
            this.work = work ?? (() => Task.CompletedTask);
            this.minMs = Math.Max(0, minVisibleMs);
            this.caption = string.IsNullOrWhiteSpace(text) ? "Загрузка..." : text;

            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;
            DoubleBuffered = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(20, 20, 24);

            TryLoadBg();
            BuildUI();

            Shown += async (s, e) =>
            {
                var sw = Stopwatch.StartNew();
                try { await work(); }
                catch { }
                finally
                {
                    int left = minMs - (int)sw.ElapsedMilliseconds;
                    if (left > 0) await Task.Delay(left);
                    Close();
                }
            };

            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) e.SuppressKeyPress = true; };
            Resize += (s, e) => LayoutCenter();
        }

        private void TryLoadBg()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var d = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                {
                    string fonDir = Path.Combine(d.FullName, "fon");
                    if (!Directory.Exists(fonDir)) continue;

                    string[] pref = { "Armagedon", "Armageddon", "Fon_Game" };
                    string[] exts = { ".jpg", ".jpeg", ".png", ".bmp" };
                    foreach (var name in pref)
                        foreach (var ext in exts)
                        {
                            var p = Path.Combine(fonDir, name + ext);
                            if (File.Exists(p)) { bg = LoadNoLock(p); return; }
                        }

                    var any = Directory.GetFiles(fonDir, "*.*", SearchOption.TopDirectoryOnly)
                               .FirstOrDefault(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));
                    if (any != null) { bg = LoadNoLock(any); return; }
                }
            }
            catch { }
        }

        private static Image LoadNoLock(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var tmp = Image.FromStream(fs);
            return new Bitmap(tmp);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.Clear(Color.FromArgb(20, 20, 24));

            if (bg != null)
            {
                var dst = GetCoverRect(ClientSize, bg.Size);
                g.DrawImage(bg, dst);
            }

            using var gp = new GraphicsPath();
            gp.AddRectangle(new Rectangle(0, 0, Width, Height));
            using var vignette = new PathGradientBrush(gp)
            {
                CenterColor = Color.FromArgb(0, 0, 0, 0),
                SurroundColors = new[] { Color.FromArgb(190, 0, 0, 0) },
                CenterPoint = new PointF(Width / 2f, Height / 2f)
            };
            g.FillRectangle(vignette, ClientRectangle);
        }

        private void BuildUI()
        {
            shade = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(40, 10, 10, 14) };
            Controls.Add(shade);

            rune = new RuneLoader
            {
                Size = new Size(220, 220),
                ColorMajor = Color.FromArgb(255, 215, 90),
                ColorMinor = Color.FromArgb(255, 170, 70),
                ColorGlow = Color.FromArgb(255, 235, 170)
            };
            shade.Controls.Add(rune);

            lbl = new Label
            {
                AutoSize = false,
                Text = caption,
                Width = Math.Min(560, ClientSize.Width - 40),
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 16f),
                BackColor = Color.Transparent
            };
            shade.Controls.Add(lbl);

            LayoutCenter();
        }

        private void LayoutCenter()
        {
            if (rune == null || lbl == null) return;

            rune.Left = (ClientSize.Width - rune.Width) / 2;
            rune.Top = (ClientSize.Height - rune.Height) / 2 - 20;

            lbl.Width = Math.Min(560, ClientSize.Width - 40);
            lbl.Left = (ClientSize.Width - lbl.Width) / 2;
            lbl.Top = rune.Bottom + 10;
        }

        private static Rectangle GetCoverRect(Size container, Size content)
        {
            if (content.Width <= 0 || content.Height <= 0)
                return new Rectangle(0, 0, container.Width, container.Height);

            float scale = Math.Max(container.Width / (float)content.Width,
                                   container.Height / (float)content.Height);
            int w = (int)Math.Ceiling(content.Width * scale);
            int h = (int)Math.Ceiling(content.Height * scale);
            return new Rectangle((container.Width - w) / 2, (container.Height - h) / 2, w, h);
        }

        public static void ShowFullscreenWhile(IWin32Window owner, string text, Func<Task> work, int minMs = 3000)
        {
            using var f = new LoadingForm(text, work, minMs);
            if (owner == null) f.ShowDialog();
            else f.ShowDialog(owner);
        }

        public static void ShowFullscreenFor(IWin32Window owner, string text, int durationMs = 3000)
        {
            using var f = new LoadingForm(text, () => Task.Delay(durationMs), 0);
            if (owner == null) f.ShowDialog();
            else f.ShowDialog(owner);
        }
    }

    
    internal class RuneLoader : Control
    {
        private readonly WinTimer t = new WinTimer { Interval = 16 };
        private float aOuter, aMiddle, aInner, pulse;

        public Color ColorMajor { get; set; } = Color.Gold;
        public Color ColorMinor { get; set; } = Color.Orange;
        public Color ColorGlow { get; set; } = Color.FromArgb(255, 240, 170);

        public RuneLoader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            t.Tick += (s, e) =>
            {
                aOuter += 2.2f; if (aOuter >= 360) aOuter -= 360;
                aMiddle -= 1.7f; if (aMiddle <= -360) aMiddle += 360;
                aInner += 1.1f; if (aInner >= 360) aInner -= 360;
                pulse += 0.08f; if (pulse > Math.PI * 2) pulse -= (float)(Math.PI * 2);
                Invalidate();
            };
            t.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = Width / 2f, cy = Height / 2f;

            using (var gp = new GraphicsPath())
            {
                float glowR = Math.Min(Width, Height) * 0.42f;
                gp.AddEllipse(cx - glowR, cy - glowR, glowR * 2, glowR * 2);
                using var glow = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb(90, ColorGlow),
                    SurroundColors = new[] { Color.FromArgb(0, ColorGlow) }
                };
                g.FillPath(glow, gp);
            }

            DrawArc(g, cx, cy, rad: 86, width: 8f, start: aOuter, sweep: 200f, ColorMajor, alpha: 220);
            DrawArc(g, cx, cy, rad: 86, width: 8f, start: aOuter + 220, sweep: 80f, ColorMajor, alpha: 200);

            DrawTicks(g, cx, cy, rad: 70, count: 48, len: 8f, thickness: 2f, rot: aMiddle, c: ColorMinor, alpha: 160);

            DrawArc(g, cx, cy, rad: 58, width: 4f, start: aInner, sweep: 160f, ColorGlow, alpha: 220);
            DrawArc(g, cx, cy, rad: 58, width: 3f, start: aInner + 200, sweep: 70f, ColorMinor, alpha: 200);

            DrawSparks(g, cx, cy, baseRad: 76, count: 8, baseAngle: aOuter, c: ColorGlow);

            float pulseWidth = 1.5f + 0.6f * (float)(0.5 + 0.5 * Math.Sin(pulse * 2));
            using var pen = new Pen(Color.FromArgb(140, ColorGlow), pulseWidth);
            g.DrawEllipse(pen, cx - 92, cy - 92, 184, 184);
        }

        private static void DrawArc(Graphics g, float cx, float cy, float rad, float width, float start, float sweep, Color c, int alpha = 255)
        {
            var r = new RectangleF(cx - rad, cy - rad, rad * 2, rad * 2);
            using var pen = new Pen(Color.FromArgb(alpha, c), width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(pen, r, start, sweep);
        }

        private static void DrawTicks(Graphics g, float cx, float cy, float rad, int count, float len, float thickness, float rot, Color c, int alpha)
        {
            using var pen = new Pen(Color.FromArgb(alpha, c), thickness)
            {
                StartCap = LineCap.Flat,
                EndCap = LineCap.Flat
            };
            for (int i = 0; i < count; i++)
            {
                float a = (rot + i * (360f / count)) * (float)Math.PI / 180f;
                float x1 = cx + (float)Math.Cos(a) * (rad - len / 2f);
                float y1 = cy + (float)Math.Sin(a) * (rad - len / 2f);
                float x2 = cx + (float)Math.Cos(a) * (rad + len / 2f);
                float y2 = cy + (float)Math.Sin(a) * (rad + len / 2f);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private static void DrawSparks(Graphics g, float cx, float cy, float baseRad, int count, float baseAngle, Color c)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (baseAngle + i * (360f / count)) * (float)Math.PI / 180f;
                float r = baseRad + (float)Math.Sin((baseAngle + i * 37) * Math.PI / 180f) * 2.2f;
                float x = cx + (float)Math.Cos(a) * r;
                float y = cy + (float)Math.Sin(a) * r;
                int alpha = 160 + (int)(80 * (0.5 + 0.5 * Math.Sin((baseAngle * 2 + i * 25) * Math.PI / 180)));
                float s = 4.4f;
                using var br = new SolidBrush(Color.FromArgb(alpha, c));
                g.FillEllipse(br, x - s / 2f, y - s / 2f, s, s);
            }
        }
    }
}