using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Rogalik
{
    public class StoryForm : Form
    {
        private Panel header;
        private MenuButton backBtn;
        private Label titleLbl;

        private TableLayoutPanel tlp;
        private Panel leftPanel, centerPanel, rightPanel;
        private PictureBox picAngel, picDemon;
        private Label storyLbl;

        private Bitmap angelImg, demonImg;

        public StoryForm()
        {
            Text = "Сюжет";
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;
            BackColor = Color.FromArgb(24, 24, 28);
            DoubleBuffered = true;
            KeyPreview = true;

            BuildUI();

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    CloseToMenuWithLoading();
                }
            };

            Resize += (s, e) => LayoutImages();
            FormClosed += (s, e) =>
            {
                picAngel.Image?.Dispose();
                picDemon.Image?.Dispose();
                angelImg?.Dispose();
                demonImg?.Dispose();
            };
        }

        private void CloseToMenuWithLoading()
        {
            LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
            Close();
        }

        private void BuildUI()
        {
            header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(24, 24, 28)
            };
            Controls.Add(header);

            backBtn = new MenuButton
            {
                Text = "← Назад",
                Width = 180,
                Height = 44,
                Left = 12,
                Top = (header.Height - 44) / 2,
                Font = new Font("Segoe UI Semibold", 12f)
            };
            backBtn.Click += (s, e) => CloseToMenuWithLoading();
            header.Controls.Add(backBtn);

            titleLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Сюжет",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(titleLbl);
            titleLbl.SendToBack();

            tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(24, 24, 28)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            Controls.Add(tlp);

            leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(36, 36, 44) };
            centerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(36, 36, 44), Padding = new Padding(24, 28, 24, 28) };
            rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(36, 36, 44) };

            tlp.Controls.Add(leftPanel, 0, 0);
            tlp.Controls.Add(centerPanel, 1, 0);
            tlp.Controls.Add(rightPanel, 2, 0);

            picAngel = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(28, 28, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            leftPanel.Controls.Add(picAngel);

            picDemon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(28, 28, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            rightPanel.Controls.Add(picDemon);

            storyLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text =
                    "Когда свет снизошёл с небес, и наступал армагедон, ужас творился на земле, люди не знали что делать, " +
                    "но не все из них... Ангелы сошли с неба и началась война Бога и Сатаны. " +
                    "Аранбо — ангел Бога, посланный с небес сражаться с демонами до окончания армагедона. " +
                    "Ардесат — демон Сатаны, выбравшийся из пучин ада, пришёл творить зло на земле, " +
                    "повелевая своим отрядом демонов, пока его время и время Сатаны не истечёт до суда.",
                ForeColor = Color.Gainsboro,
                BackColor = Color.FromArgb(26, 26, 30),
                Font = new Font("Segoe UI", 22, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Padding = new Padding(20),
                AutoEllipsis = false
            };
            centerPanel.Controls.Add(storyLbl);

            angelImg = LoadImageUnlocked(@"StoryCharacters\Angel.png");
            demonImg = LoadImageUnlocked(@"StoryCharacters\Demon.png");
            if (angelImg != null) angelImg = CropTransparent(angelImg);
            if (demonImg != null) demonImg = CropTransparent(demonImg);
            // Фолбэки из Assets
            if (angelImg == null) angelImg = LoadImageUnlocked(@"Assets\angel.png");
            if (demonImg == null) demonImg = LoadImageUnlocked(@"Assets\demon.png");

            picAngel.Image = angelImg ?? MakePlaceholder("angel");
            picDemon.Image = demonImg ?? MakePlaceholder("demon");

            LayoutImages();
        }

        private void LayoutImages()
        {
            int m = 16;

            int lw = Math.Max(0, leftPanel.ClientSize.Width - m * 2);
            int lh = Math.Max(0, leftPanel.ClientSize.Height - m * 2);
            int rw = Math.Max(0, rightPanel.ClientSize.Width - m * 2);
            int rh = Math.Max(0, rightPanel.ClientSize.Height - m * 2);

            int boxW = (int)(Math.Min(lw, rw) * 0.9);
            int boxH = (int)(Math.Min(lh, rh) * 0.9);
            boxW = Math.Max(120, boxW);
            boxH = Math.Max(160, boxH);

            picAngel.SetBounds(
                (leftPanel.ClientSize.Width - boxW) / 2,
                (leftPanel.ClientSize.Height - boxH) / 2,
                boxW, boxH);

            picDemon.SetBounds(
                (rightPanel.ClientSize.Width - boxW) / 2,
                (rightPanel.ClientSize.Height - boxH) / 2,
                boxW, boxH);
        }

        private static Bitmap LoadImageUnlocked(string rel)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] c =
                {
                    Path.Combine(baseDir, rel),
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..", rel)),
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..", rel)),
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..", rel)),
                };
                foreach (var p in c)
                {
                    if (File.Exists(p))
                    {
                        using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                var data = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int w = src.Width, h = src.Height, stride = Math.Abs(data.Stride);
                    int bytes = stride * h;
                    var buf = new byte[bytes];
                    Marshal.Copy(data.Scan0, buf, 0, bytes);

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
                        return src.Clone(crop, PixelFormat.Format32bppArgb);
                    }
                    return (Bitmap)src.Clone();
                }
                finally
                {
                    src.UnlockBits(data);
                }
            }
            catch { return (Bitmap)src.Clone(); }
        }

        private static Image MakePlaceholder(string text)
        {
            var bmp = new Bitmap(320, 480);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(32, 32, 36));
                using var br = new SolidBrush(Color.Gainsboro);
                using var font = new Font("Segoe UI", 14, FontStyle.Bold);
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, font, br, new RectangleF(0, 0, bmp.Width, bmp.Height), fmt);
            }
            return bmp;
        }
    }
}