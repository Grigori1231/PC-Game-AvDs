using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Rogalik
{
    public enum AbilityKind { Активный, Пассивный }

    public class AbilityDef
    {
        public string Id;
        public string Name;
        public AbilityKind Kind;
        public string Types;
        public string ShortText;
        public List<StatLine> Stats = new();
        public string Icon;
        public int ShowOrderBadge = 0;
    }

    public class StatLine
    {
        public string Label;
        public string Value;
        public Color? ValueColor = null;
        public StatLine() { }
        public StatLine(string label, string value, Color? color = null)
        { Label = label; Value = value; ValueColor = color; }
    }

    public static class AbilityCatalog
    {
        private static readonly Random rng = new Random();

        private static string SkillsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
            Path.Combine(baseDir, "Skills"),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\Skills")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Skills")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\Skills")),
        };
            foreach (var c in candidates) if (Directory.Exists(c)) return c;
            return Path.Combine(baseDir, "Skills");
        }

        private static string CardsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
            Path.Combine(baseDir, "Assets", "cards"),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\Assets\cards")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Assets\cards")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\Assets\cards")),
        };
            foreach (var c in candidates) if (Directory.Exists(c)) return c;
            return Path.Combine(baseDir, "Assets", "cards");
        }

        public static Image GetIcon(AbilityDef ab)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ab.Icon) && Path.IsPathRooted(ab.Icon) && File.Exists(ab.Icon))
                    return Image.FromFile(ab.Icon);
            }
            catch { }

            var m = Regex.Match(ab.Id ?? "", @"^ab(\d{1,2})$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string n = m.Groups[1].Value;
                string root = SkillsRoot();
                string[] files =
                {
                Path.Combine(root, $"{n}_skill.jpg"),
                Path.Combine(root, $"{n}_skill.png")
            };
                foreach (var fp in files)
                {
                    try { if (File.Exists(fp)) return Image.FromFile(fp); } catch { }
                }
            }

            if (!string.IsNullOrWhiteSpace(ab.Icon))
            {
                var p1 = Path.Combine(SkillsRoot(), ab.Icon);
                var p2 = Path.Combine(CardsRoot(), ab.Icon);
                try { if (File.Exists(p1)) return Image.FromFile(p1); } catch { }
                try { if (File.Exists(p2)) return Image.FromFile(p2); } catch { }
            }

            var bmp = new Bitmap(96, 96);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(48, 48, 56));
            using var br = new SolidBrush(Color.White);
            using var f = new Font("Segoe UI", 9);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("icon", f, br, new RectangleF(0, 0, 96, 96), fmt);
            return bmp;
        }

        private static Color Green => Color.FromArgb(0, 220, 100);

        public static readonly List<AbilityDef> All = new List<AbilityDef>
    {
        new AbilityDef { Id="ab1",  Name="Сотрясение земли", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Толчки по области под собой.",
            Stats = new(){ new("Частота толчков:","0,5сек.",Green), new("Длительность:","5,0сек.",Green), new("Урон:","3",Green), new("Перезарядка:","10сек.",Green)} },

        new AbilityDef { Id="ab2",  Name="Излучение света", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Аура света в небольшой области перед собой.",
            Stats = new(){ new("Частота излучения:","1сек.",Green), new("Длительность:","6,0сек.",Green), new("Урон:","4",Green), new("Перезарядка:","12сек.",Green)} },

        new AbilityDef { Id="ab3",  Name="Прорубающий удар меча", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Малый пробивающий снаряд.",
            Stats = new(){ new("Снарядов:","1",Green), new("Урон:","5",Green), new("Перезарядка:","3сек.",Green)} },

        new AbilityDef { Id="ab4",  Name="Взмах меча", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Полукруговой удар.",
            Stats = new(){ new("Урон:","6",Green), new("Перезарядка:","5сек.",Green)} },

        new AbilityDef { Id="ab5",  Name="Выстрел снарядами", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="8 снарядов вокруг.",
            Stats = new(){ new("Снарядов:","8",Green), new("Урон:","3",Green), new("Перезарядка:","6сек.",Green)} },

        new AbilityDef { Id="ab6",  Name="Выстрел ядром", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Тяжёлый снаряд вперёд.",
            Stats = new(){ new("Урон:","5",Green), new("Перезарядка:","4сек.",Green)} },

        new AbilityDef { Id="ab7",  Name="Удар щитом", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Короткий конус с отталкиванием.",
            Stats = new(){ new("Длительность:","2сек.",Green), new("Урон:","3",Green), new("Перезарядка:","5сек.",Green)} },

        new AbilityDef { Id="ab8",  Name="Взмах мечом вокруг себя", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Круговой взмах.",
            Stats = new(){ new("Урон:","4",Green), new("Перезарядка:","6сек.",Green)} },

        new AbilityDef { Id="ab9",  Name="Волны души", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Три волны насквозь в цель.",
            Stats = new(){ new("Урон:","3",Green), new("Перезарядка:","8сек.",Green)} },

        new AbilityDef { Id="ab10", Name="Запуск духовного снаряда", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Цепной снаряд на 4 прыжка.",
            Stats = new(){ new("Урон:","2",Green), new("Перезарядка:","5сек.",Green)} },

        new AbilityDef { Id="ab11", Name="Вырывающийся свет", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Зона света: тики урона и взрыв.",
            Stats = new(){ new("Частота:","0,7сек.",Green), new("Длительность:","3сек.",Green), new("Тик урон:","2",Green), new("Взрыв урон:","8",Green), new("Перезарядка:","10сек.",Green)} },

        new AbilityDef { Id="ab12", Name="Путь в бездну", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Малая бездна — мгновенная смерть.",
            Stats = new(){ new("Длительность:","5сек.",Green), new("Перезарядка:","20сек.",Green)} },

        new AbilityDef { Id="ab13", Name="Благословение", Kind=AbilityKind.Активный, Types="Благодатный, Длительный, Бафф",
            ShortText="Урон навыков +50% на 10 сек.",
            Stats = new(){ new("Бонус урона:","50%",Green), new("Длительность:","10сек.",Green), new("Перезарядка:","30сек.",Green)} },

        new AbilityDef { Id="ab14", Name="Просьба богу", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Большая вспышка по области.",
            Stats = new(){ new("Урон:","50",Green), new("Перезарядка:","20сек.",Green)} },

        new AbilityDef { Id="ab15", Name="Дождь из стрел", Kind=AbilityKind.Активный, Types="Святой, Длительный",
            ShortText="Дождь из стрел в ближайшего врага.",
            Stats = new(){ new("Урон:","2",Green), new("Частота:","0,3сек.",Green), new("Длительность:","3сек.",Green), new("Перезарядка:","10сек.",Green)} },

        new AbilityDef { Id="ab16", Name="Заряд снарядов", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="20 снарядов перед собой.",
            Stats = new(){ new("Снарядов:","20",Green), new("Урон:","2",Green), new("Перезарядка:","8сек.",Green)} },

        new AbilityDef { Id="ab17", Name="Пронзающая стрела", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="Мощная стрела насквозь в ближайшего врага.",
            Stats = new(){ new("Урон:","8",Green), new("Перезарядка:","5сек.",Green)} },

        new AbilityDef { Id="ab18", Name="Лучи с небес", Kind=AbilityKind.Активный, Types="Благодатный, Длительный",
            ShortText="Случайные лучи по области.",
            Stats = new(){ new("Частота:","0,5сек.",Green), new("Длительность:","8сек.",Green), new("Урон:","8",Green), new("Перезарядка:","20сек.",Green)} },

        new AbilityDef { Id="ab19", Name="Тройной выстрел", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="3 стрелы в ближайшего врага.",
            Stats = new(){ new("Снарядов:","3",Green), new("Урон:","4",Green), new("Перезарядка:","3сек.",Green)} },

        new AbilityDef { Id="ab20", Name="Усиленный отскок атаки", Kind=AbilityKind.Активный, Types="Благодатный",
            ShortText="Мощный снаряд с отскоком.",
            Stats = new(){ new("Урон:","15",Green), new("Перезарядка:","10сек.",Green)} },

        new AbilityDef { Id="ab21", Name="Обстрел из стрел", Kind=AbilityKind.Активный, Types="Святой, Длительный",
            ShortText="3 сек. стрелы в случайных направлениях.",
            Stats = new(){ new("Длительность:","3,0сек.",Green), new("Интервал:","0,2сек.",Green), new("Урон:","4",Green), new("Перезарядка:","10сек.",Green) } },

        new AbilityDef { Id="ab22", Name="Подъём мечей", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="5 мечей летят к ближайшим врагам.",
            Stats = new(){ new("Мечей:","5",Green), new("Урон:","5",Green), new("Перезарядка:","14сек.",Green) } },

        new AbilityDef { Id="ab23", Name="Духовный взрыв", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="Взрыв у ближайшего врага с затуханием по дистанции.",
            Stats = new(){ new("Урон:","20",Green), new("Перезарядка:","25сек.",Green) } },

        new AbilityDef { Id="ab24", Name="Духовная сфера", Kind=AbilityKind.Активный, Types="Святой, Длительный",
            ShortText="Сфера у ближайшего врага, периодический урон по площади.",
            Stats = new(){ new("Длительность:","2,0сек.",Green), new("Интервал:","0,5сек.",Green), new("Тик урон:","3",Green), new("Перезарядка:","8сек.",Green) } },

        new AbilityDef { Id="ab25", Name="Два меча", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="2 меча летят к ближайшим врагам.",
            Stats = new(){ new("Мечей:","2",Green), new("Урон:","4",Green), new("Перезарядка:","5сек.",Green) } },

        new AbilityDef { Id="ab26", Name="Духовная стяжка", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="Притягивает всех врагов и наносит урон.",
            Stats = new(){ new("Урон:","4",Green), new("Перезарядка:","20сек.",Green) } },

        new AbilityDef { Id="ab27", Name="Дугообразные выстрелы", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="2 дугообразных выстрела в ближайшую цель.",
            Stats = new(){ new("Снарядов:","2",Green), new("Урон:","3",Green), new("Перезарядка:","6сек.",Green) } },

        new AbilityDef { Id="ab28", Name="Духовный удар", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="Если враг близко — удар с сильным отталкиванием.",
            Stats = new(){ new("Урон:","4",Green), new("Перезарядка:","3сек.",Green) } },

        new AbilityDef { Id="ab29", Name="Хаотичный снаряд", Kind=AbilityKind.Активный, Types="Святой",
            ShortText="Духовный снаряд двигается хаотично.",
            Stats = new(){ new("Урон:","4",Green), new("Перезарядка:","4сек.",Green) } },

        new AbilityDef { Id="ab30", Name="Духовная аура", Kind=AbilityKind.Активный, Types="Святой, Длительный",
            ShortText="Аура вокруг игрока, периодический урон рядом.",
            Stats = new(){ new("Длительность:","5,0сек.",Green), new("Интервал:","0,3сек.",Green), new("Тик урон:","3",Green), new("Перезарядка:","12сек.",Green) } },
    };

        public static List<AbilityDef> PickRandom(int count)
        {
            var list = All.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list.Take(Math.Max(0, Math.Min(count, list.Count))).ToList();
        }
    }

    public class AbilityPicker
    {
        private readonly Control host;
        private readonly Panel overlay;
        private readonly Panel cardsPanel;
        private readonly Label title;
        private Action<AbilityDef> onPick;

        private readonly Color gold = Color.FromArgb(230, 170, 70);
        private readonly Color cyan = Color.FromArgb(80, 200, 220);

        public AbilityPicker(Control host)
        {
            this.host = host;

            overlay = new Panel
            {
                Visible = false,
                BackColor = Color.FromArgb(170, 0, 0, 0),
                Dock = DockStyle.Fill
            };
            host.Controls.Add(overlay);
            overlay.BringToFront();

            title = new Label
            {
                Dock = DockStyle.Top,
                Height = 86,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Text = "Выберите способность"
            };
            overlay.Controls.Add(title);

            cardsPanel = new Panel { Dock = DockStyle.Fill };
            overlay.Controls.Add(cardsPanel);
        }

        public bool IsOpen => overlay.Visible;

        public void ShowChoices(List<AbilityDef> choices, Action<AbilityDef> onPick)
        {
            this.onPick = onPick;
            overlay.Visible = true;
            overlay.BringToFront();
            cardsPanel.Controls.Clear();

            bool allBuffs = choices.All(c => c.Id != null && c.Id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase));
            title.Text = allBuffs ? "Выберите бафф" : "Выберите способность";

            int cardWidth = Math.Max(360, Math.Min(440, host.ClientSize.Width / Math.Max(1, choices.Count) - 30));
            int cardHeight = Math.Max(420, Math.Min(560, host.ClientSize.Height - 160));
            int spacing = 24;

            int totalW = choices.Count * cardWidth + (choices.Count - 1) * spacing;
            int startX = Math.Max(10, (host.ClientSize.Width - totalW) / 2);
            int top = Math.Max(30, (host.ClientSize.Height - cardHeight) / 2);

            for (int i = 0; i < choices.Count; i++)
            {
                var ab = choices[i]; ab.ShowOrderBadge = i + 1;
                var card = BuildCard(ab, cardWidth, cardHeight);
                card.Left = startX + i * (cardWidth + spacing);
                card.Top = top;
                cardsPanel.Controls.Add(card);
            }
        }

        public void HideOverlay()
        {
            cardsPanel.Controls.Clear();
            overlay.Visible = false;
            host.Focus();
            if (host is Form f) f.Select();
        }

        private Panel BuildCard(AbilityDef ab, int w, int h)
        {
            bool isBuff = ab != null && ab.Id != null && ab.Id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase);
            Color frame = isBuff ? gold : cyan;

            var card = new Panel
            {
                Width = w,
                Height = h,
                BackColor = Color.FromArgb(34, 34, 42),
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using var penBorder = new Pen(frame, 3);
                g.DrawRectangle(penBorder, 1, 1, card.Width - 3, card.Height - 3);

                using var topPen = new Pen(Color.FromArgb(90, 90, 98), 2);
                g.DrawLine(topPen, 12, 68, card.Width - 12, 68);
            };

            var badge = new Label
            {
                Text = ab.ShowOrderBadge.ToString(),
                AutoSize = false,
                Width = 30,
                Height = 30,
                Left = 10,
                Top = 6,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 58),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var br = new SolidBrush(Color.FromArgb(60, 60, 70));
                g.FillEllipse(br, 0, 0, badge.Width - 1, badge.Height - 1);
                using var pen = new Pen(Color.FromArgb(100, 100, 110), 2);
                g.DrawEllipse(pen, 1, 1, badge.Width - 3, badge.Height - 3);
                TextRenderer.DrawText(g, badge.Text, badge.Font, new Rectangle(0, 0, badge.Width, badge.Height), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            card.Controls.Add(badge);

            Image iconImg = isBuff ? BuffCatalog.GetIconForAbilityLike(ab) : AbilityCatalog.GetIcon(ab);

            var icon = new PictureBox
            {
                Width = 96,
                Height = 96,
                Left = (w - 96) / 2,
                Top = 10,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = iconImg
            };
            icon.Paint += (s, e) =>
            {
                using var pen = new Pen(frame, 3);
                e.Graphics.DrawRectangle(pen, 1, 1, icon.Width - 3, icon.Height - 3);
            };
            card.Controls.Add(icon);

            int y = icon.Bottom + 6;

            var name = new Label
            {
                Left = 14,
                Top = y,
                Width = w - 28,
                Height = 32,
                Text = ab.Name,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };
            card.Controls.Add(name);
            y = name.Bottom - 2;

            var kind = new Label
            {
                Left = 14,
                Top = y,
                Width = w - 28,
                Height = 22,
                Text = ab.Kind == AbilityKind.Активный ? "Активный навык" : "Пассивный навык",
                ForeColor = frame,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold)
            };
            card.Controls.Add(kind);
            y = kind.Bottom + 6;

            var desc = new Label
            {
                Left = 14,
                Top = y,
                Width = w - 28,
                Height = 60,
                Text = ab.ShortText,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10)
            };
            card.Controls.Add(desc);
            y = desc.Bottom + 6;

            var typeLbl = new Label
            {
                Left = 14,
                Top = y,
                Width = w - 28,
                Height = 38,
                Text = "Тип: " + ab.Types,
                ForeColor = Color.FromArgb(80, 200, 220),
                Font = new Font("Segoe UI", 10)
            };
            card.Controls.Add(typeLbl);
            y = typeLbl.Bottom + 8;

            var stats = new TableLayoutPanel
            {
                Left = 14,
                Top = y,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            for (int i = 0; i < ab.Stats.Count; i++)
            {
                stats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var st = ab.Stats[i];

                var l = new Label
                {
                    AutoSize = true,
                    Text = st.Label,
                    ForeColor = Color.FromArgb(210, 210, 210),
                    Font = new Font("Segoe UI", 10),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,
                    Margin = new Padding(0, 2, 0, 2)
                };
                var r = new Label
                {
                    AutoSize = true,
                    Text = st.Value,
                    ForeColor = st.ValueColor ?? Color.FromArgb(0, 220, 100),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    TextAlign = ContentAlignment.TopRight,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    Margin = new Padding(0, 2, 0, 2)
                };

                stats.Controls.Add(l, 0, i);
                stats.Controls.Add(r, 1, i);
            }
            card.Controls.Add(stats);

            var btn = new Button
            {
                Text = "Выбрать",
                Width = w - 28,
                Height = 42,
                Left = 14,
                Top = h - 14 - 42,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 82);
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) => onPick?.Invoke(ab);
            card.Controls.Add(btn);

            return card;
        }
    }
}