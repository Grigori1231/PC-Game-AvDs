using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Rogalik
{
    public class EnemyTypesForm : Form
    {
        private Panel header;
        private Label title;
        private MenuButton backBtn;

        private Panel cardsHost;

        public EnemyTypesForm()
        {
            Text = "Типы врагов";
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.PrimaryScreen.Bounds;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.FromArgb(24, 24, 28);

            BuildUI();

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    CloseToMenuWithLoading();
                }
            };
            Resize += (s, e) => LayoutCards();
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
                Height = 72,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(24, 24, 28)
            };
            Controls.Add(header);

            backBtn = new MenuButton
            {
                Text = "← Назад",
                Width = 220,
                Height = 56,
                Left = 16,
                Top = (header.Height - 56) / 2,
                Font = new Font("Segoe UI Semibold", 14f)
            };
            backBtn.Click += (s, e) => CloseToMenuWithLoading();
            header.Controls.Add(backBtn);

            title = new Label
            {
                AutoSize = false,
                Text = "Типы врагов",
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.White,
                Height = header.Height,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            header.Controls.Add(title);
            title.SendToBack();

            cardsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 24, 28),
                Padding = new Padding(24)
            };
            Controls.Add(cardsHost);

            var cardCommon = BuildEnemyCard(
                name: "Обычный демон",
                desc: "Преследует игрока и наносит урон при контакте.",
                img: TryFindPreview("demon_common"),
                stats: new (string, string)[]
                {
                    ("Здоровье", "10"),
                    ("Скорость", "2.2"),
                    ("Урон (контакт)", "10"),
                    ("Дистанция", "ближняя")
                }
            );

            var cardStrong = BuildEnemyCard(
                name: "Демон в тяжёлых доспехах",
                desc: "Более медленный, чем обычный, но крепкий.",
                img: TryFindPreview("demon_strong"),
                stats: new (string, string)[]
                {
                    ("Здоровье", "15"),
                    ("Скорость", "1.6"),
                    ("Урон (контакт)", "10"),
                    ("Дистанция", "ближняя")
                }
            );

            var cardArcher = BuildEnemyCard(
                name: "Демон‑лучник",
                desc: "Атакует с расстояния стрелами.",
                img: TryFindPreview("demon_archer"),
                stats: new (string, string)[]
                {
                    ("Здоровье", "7"),
                    ("Скорость", "2.0"),
                    ("Урон (стрела)", "5"),
                    ("Дистанция", "дальняя")
                }
            );

            var cardMage = BuildEnemyCard(
                name: "Демон‑маг",
                desc: "Выпускает магическую сферу: 2 сек. наводится, затем летит по инерции.",
                img: TryFindPreview("demon_mage"),
                stats: new (string, string)[]
                {
                    ("Здоровье", "5"),
                    ("Скорость", "1.5"),
                    ("Урон (сфера)", "12"),
                    ("Дистанция", "дальняя")
                }
                        );

            cardsHost.Controls.Add(cardCommon);
            cardsHost.Controls.Add(cardStrong);
            cardsHost.Controls.Add(cardArcher);
            cardsHost.Controls.Add(cardMage);

            LayoutCards();
        }

        private Panel BuildEnemyCard(string name, string desc, Image img, (string, string)[] stats)
        {
            var card = new Panel
            {
                Width = 420,
                Height = 520,
                BackColor = Color.FromArgb(36, 36, 44),
                BorderStyle = BorderStyle.FixedSingle
            };

            var nameLbl = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Height = 42,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.FromArgb(36, 36, 44)
            };
            card.Controls.Add(nameLbl);

            var pic = new PictureBox
            {
                Width = 380,
                Height = 260,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(28, 28, 32),
                Image = img
            };
            pic.Left = (card.Width - pic.Width) / 2;
            pic.Top = nameLbl.Bottom + 8;
            card.Controls.Add(pic);

            var descLbl = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gainsboro,
                AutoSize = false,
                Width = card.Width - 24,
                Height = 48,
                Left = 12,
                Top = pic.Bottom + 8
            };
            card.Controls.Add(descLbl);

            var table = new TableLayoutPanel
            {
                Left = 12,
                Top = descLbl.Bottom + 6,
                Width = card.Width - 24,
                Height = 120,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            foreach (var (k, v) in stats)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var lk = new Label
                {
                    Text = k + ":",
                    ForeColor = Color.Gainsboro,
                    Font = new Font("Segoe UI", 10),
                    AutoSize = true
                };
                var lv = new Label
                {
                    Text = v,
                    ForeColor = Color.FromArgb(230, 200, 90),
                    Font = new Font("Segoe UI Semibold", 10),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleRight,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top
                };
                table.Controls.Add(lk);
                table.Controls.Add(lv);
            }
            card.Controls.Add(table);

            return card;
        }

        private void LayoutCards()
        {
            if (cardsHost == null || cardsHost.Controls.Count == 0) return;

            int n = cardsHost.Controls.Count;
            int gap = 20;
            int totalW = cardsHost.Padding.Left + cardsHost.Padding.Right;
            for (int i = 0; i < n; i++)
                totalW += ((Panel)cardsHost.Controls[i]).Width;
            totalW += (n - 1) * gap;

            int startX = Math.Max(cardsHost.Padding.Left, (cardsHost.ClientSize.Width - totalW) / 2);
            int y = Math.Max(cardsHost.Padding.Top, (cardsHost.ClientSize.Height - ((Panel)cardsHost.Controls[0]).Height) / 2 + 12);

            int x = startX;
            for (int i = 0; i < n; i++)
            {
                var card = (Panel)cardsHost.Controls[i];
                card.Top = y;
                card.Left = x;
                x += card.Width + gap;
            }
        }

        private static string GifsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "gifs_animation"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\gifs_animation")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\gifs_animation")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\gifs_animation")),
            };
            return candidates.FirstOrDefault(Directory.Exists) ?? Path.Combine(baseDir, "gifs_animation");
        }

        private static Image TryFindPreview(string enemyFolder)
        {
            string root = GifsRoot();
            string folder = Path.Combine(root, enemyFolder);
            if (!Directory.Exists(folder))
                return MakePlaceholder(enemyFolder);
            try
            {
                string[] priority = enemyFolder switch
                {
                    "demon_archer" => new[] { "walk_archer_down.gif", "walk_archer_down.png", "walk_archer_down.jpg" },
                    "demon_strong" => new[] { "walk_strong_down.gif", "walk_strong_down.png", "walk_strong_down.jpg" },
                    "demon_common" => new[] { "walk_down_d.gif", "walk_down.gif" },
                    "demon_mage" => new[] { "walk_down_mage.gif", "walk_down_mage.png", "spellcast_down.gif", "spellcast_down.png" },
                    _ => Array.Empty<string>()
                };

                foreach (var fn in priority)
                {
                    var p = Path.Combine(folder, fn);
                    if (File.Exists(p)) return LoadNoLock(p);
                }

                var anyWalk = Directory.EnumerateFiles(folder, "walk*.*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".gif" || ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                    });
                if (anyWalk != null) return LoadNoLock(anyWalk);

                var anyCast = Directory.EnumerateFiles(folder, "spellcast*.*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".gif" || ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                    });
                if (anyCast != null) return LoadNoLock(anyCast);

                var any = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".gif" || ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                    });
                if (any != null) return LoadNoLock(any);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Mage preview error: " + ex);
            }

            return MakePlaceholder(enemyFolder);
        }

        private static Image MakePlaceholder(string text)
        {
            var bmp = new Bitmap(320, 240);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(28, 28, 32));
                using var br = new SolidBrush(Color.Gainsboro);
                using var f = new Font("Segoe UI", 12, FontStyle.Bold);
                var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(text, f, br, new RectangleF(0, 0, bmp.Width, bmp.Height), fmt);
            }
            return bmp;
        }

        private static System.Drawing.Image LoadNoLock(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var img = System.Drawing.Image.FromStream(fs);
            return new Bitmap(img);
        }
    }
}