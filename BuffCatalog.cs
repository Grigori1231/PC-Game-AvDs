using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Rogalik
{
    public class BuffDef
    {
        public string Id;
        public string Name;
        public string ShortText;
        public List<StatLine> Stats = new();
        public string Icon;
    }

    public static class BuffCatalog
    {
        private static readonly Random rng = new Random();

        private static string BuffsRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string[] folders =
            {
            "Buffs",
            "Bufs",
            Path.Combine("Assets","buffs")
        };

            foreach (var f in folders)
            {
                string[] candidates =
                {
                Path.Combine(baseDir, f),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..", f)),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..", f)),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..", f)),
            };
                foreach (var c in candidates)
                    if (Directory.Exists(c)) return c;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string TryFindIconPath(string root, string fileNameOrStem)
        {
            if (string.IsNullOrWhiteSpace(root)) return null;
            if (!Directory.Exists(root)) return null;

            string direct = Path.Combine(root, fileNameOrStem);
            if (File.Exists(direct)) return direct;

            string stem = Path.GetFileNameWithoutExtension(fileNameOrStem);
            string[] exts = { ".png", ".jpg", ".jpeg", ".bmp" };

            foreach (var ext in exts)
            {
                string p = Path.Combine(root, stem + ext);
                if (File.Exists(p)) return p;
            }

            try
            {
                var files = Directory.EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                foreach (var f in files)
                {
                    if (string.Equals(Path.GetFileNameWithoutExtension(f), stem, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }

            return null;
        }

        private static Image MakePlaceholder(string text)
        {
            var bmp = new Bitmap(96, 96);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(48, 48, 56));
            using var br = new SolidBrush(Color.White);
            using var f = new Font("Segoe UI", 9, FontStyle.Bold);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(string.IsNullOrWhiteSpace(text) ? "buff" : text, f, br, new RectangleF(0, 0, bmp.Width, bmp.Height), fmt);
            return bmp;
        }

        public static Image GetIcon(BuffDef bf)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(bf.Icon))
                {
                    string root = BuffsRoot();
                    string found = TryFindIconPath(root, bf.Icon);
                    if (found != null) return Image.FromFile(found);

                    if (File.Exists(bf.Icon)) return Image.FromFile(bf.Icon);
                }
            }
            catch { }
            return MakePlaceholder(bf.Name);
        }

        public static Image GetIconForAbilityLike(AbilityDef ab)
        {
            if (ab?.Id == null) return null;
            if (!ab.Id.StartsWith("bf_", StringComparison.OrdinalIgnoreCase)) return null;
            var bf = All.FirstOrDefault(b => string.Equals(b.Id, ab.Id, StringComparison.OrdinalIgnoreCase));
            return bf == null ? null : GetIcon(bf);
        }

        public static AbilityDef ToAbilityCard(BuffDef bf)
        {
            return new AbilityDef
            {
                Id = bf.Id,
                Name = bf.Name,
                Kind = AbilityKind.Пассивный,
                Types = "Бафф",
                ShortText = bf.ShortText,
                Stats = bf.Stats,
                Icon = bf.Icon
            };
        }

        private static Color Green => Color.FromArgb(0, 220, 100);

        public static readonly List<BuffDef> All = new List<BuffDef>
    {
        new BuffDef
        {
            Id = "bf_hp",
            Name = "Укрепление тела",
            ShortText = "Увеличивает максимальное здоровье.",
            Stats = new()
            {
                new StatLine("Макс. здоровье:", "+30%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_hp"
        },
        new BuffDef
        {
            Id = "bf_speed",
            Name = "Крылья ветра",
            ShortText = "Скорость передвижения повышена.",
            Stats = new()
            {
                new StatLine("Скорость:", "+10%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_speed"
        },
        new BuffDef
        {
            Id = "bf_xp",
            Name = "Мудрость",
            ShortText = "Получаемый опыт увеличен.",
            Stats = new()
            {
                new StatLine("Опыт:", "+25%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_xp"
        },
        new BuffDef
        {
            Id = "bf_armor",
            Name = "Железная кожа",
            ShortText = "Снижает получаемый урон.",
            Stats = new()
            {
                new StatLine("Броня:", "+2", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_armor"
        },
        new BuffDef
        {
            Id = "bf_crit_chance",
            Name = "Точность",
            ShortText = "Повышает шанс критического удара.",
            Stats = new()
            {
                new StatLine("Шанс крита:", "+5%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_crit_chance"
        },
        new BuffDef
        {
            Id = "bf_crit_mult",
            Name = "Смертельный удар",
            ShortText = "Усиливает критический урон.",
            Stats = new()
            {
                new StatLine("Крит. урон:", "+15%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_crit_mult"
        },
        new BuffDef
        {
            Id = "bf_multicast",
            Name = "Мультикаст",
            ShortText = "Шанс скастовать способность дополнительно.",
            Stats = new()
            {
                new StatLine("Шанс мультикаста:", "+10%", Green),
                new StatLine("Тип:", "Постоянный бафф")
            },
            Icon = "buff_multicast"
        },
    };

        public static List<AbilityDef> PickRandomAsCards(int count)
        {
            var list = All.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list
                .Take(Math.Max(0, Math.Min(count, list.Count)))
                .Select(ToAbilityCard)
                .ToList();
        }
    }
}