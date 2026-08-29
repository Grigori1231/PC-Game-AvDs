using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Font = System.Drawing.Font;

namespace Rogalik
{
    public partial class GameForm
    {
        private int abilitySlotSize = 64;
        private int abilitySlotSpacing = 12;
        private int abilitySlotBottomMargin = 18;
        private int abilitySlotPanelPadding = 10;
        private int abilitySlotCornerRadius = 10;

        private void DrawEnemyHpBars(Graphics g)
        {
            foreach (var e in enemies)
            {
                int bw = (int)(e.W * 0.6f);
                int bh = 6;
                int x = (int)(e.X + (e.W - bw) / 2f);
                int y = (int)(e.Y - bh - 6);

                float frac = e.MaxHP <= 0 ? 0f : Math.Max(0f, Math.Min(1f, e.HP / (float)e.MaxHP));

                using var bg = new SolidBrush(Color.FromArgb(90, 30, 30, 36));
                g.FillRectangle(bg, x, y, bw, bh);

                using var fill = new SolidBrush(Color.FromArgb(220, 210, 60, 60));
                g.FillRectangle(fill, x, y, (int)(bw * frac), bh);

                using var pen = new Pen(Color.FromArgb(160, 100, 100, 110), 1f);
                g.DrawRectangle(pen, x, y, bw, bh);
            }
        }

        private void DrawXpBar(Graphics g)
        {
            int barW = (int)(ClientSize.Width * 0.5);
            int barH = 18;
            int x = (ClientSize.Width - barW) / 2;
            int y = 10;

            using (var bg = new SolidBrush(Color.FromArgb(60, 60, 68)))
                g.FillRectangle(bg, x, y, barW, barH);

            float fracShown = Math.Max(0f, Math.Min(1f, xpFillShown));
            int fillW = (int)(barW * fracShown);

            if (fillW > 0)
            {
                var fillRect = new Rectangle(x, y, fillW, barH);
                using var br = new LinearGradientBrush(fillRect,
                    Color.FromArgb(210, 200, 60, 60),
                    Color.FromArgb(230, 220, 120, 80),
                    LinearGradientMode.Vertical);
                g.FillRectangle(br, fillRect);

                var gloss = new Rectangle(x, y, fillW, Math.Max(2, barH / 2));
                using var glossBr = new LinearGradientBrush(gloss,
                    Color.FromArgb(70, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.Vertical);
                g.FillRectangle(glossBr, gloss);

                int headX = x + fillW;
                int headLeft = Math.Max(x + 1, Math.Min(x + barW - 14, headX - 7));
                var head = new Rectangle(headLeft, y + 1, 14, barH - 2);
                using var headBr = new LinearGradientBrush(head,
                    Color.FromArgb(200, 255, 240, 140),
                    Color.FromArgb(0, 255, 240, 140),
                    LinearGradientMode.Vertical);
                g.FillRectangle(headBr, head);
            }

            float fracTarget = xpToNext == 0 ? 1f : Math.Max(0f, Math.Min(1f, (float)xp / xpToNext));
            if (fracTarget > fracShown + 0.001f)
            {
                int ghostW = (int)(barW * (fracTarget - fracShown));
                using var ghost = new SolidBrush(Color.FromArgb(60, 200, 200, 80));
                g.FillRectangle(ghost, x + fillW, y, ghostW, barH);
            }

            using var pen = new Pen(Color.FromArgb(220, 100, 40, 40), 2);
            g.DrawRectangle(pen, x, y, barW, barH);

            using var brTxt = new SolidBrush(Color.White);
            using var font = new Font("Segoe UI", 9, FontStyle.Bold);
            var txt = $"Ур. {level}  •  {xp}/{xpToNext}";
            var sz = g.MeasureString(txt, font);
            g.DrawString(txt, font, brTxt, x + (barW - sz.Width) / 2, y - 2);
        }

        private void DrawAbilitySlots(Graphics g)
        {
            int slots = MaxUniqueAbilities;
            int size = abilitySlotSize;
            int spacing = abilitySlotSpacing;

            int totalW = slots * size + (slots - 1) * spacing;
            int x = (ClientSize.Width - totalW) / 2;
            int y = ClientSize.Height - size - abilitySlotBottomMargin;

            int pad = abilitySlotPanelPadding;
            var bgRect = new Rectangle(x - pad, y - pad, totalW + pad * 2, size + pad * 2);
            using (var path = RoundRect(bgRect, abilitySlotCornerRadius))
            using (var fill = new LinearGradientBrush(bgRect,
                Color.FromArgb(40, 30, 30, 36),
                Color.FromArgb(30, 20, 20, 26),
                LinearGradientMode.Vertical))
            using (var border = new Pen(Color.FromArgb(80, 90, 98), 1.5f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using var slotBg = new SolidBrush(Color.FromArgb(40, 40, 46));
            using var slotPen = new Pen(Color.FromArgb(90, 90, 98));

            for (int i = 0; i < slots; i++)
            {
                var r = new Rectangle(x + i * (size + spacing), y, size, size);
                g.FillRectangle(slotBg, r);
                g.DrawRectangle(slotPen, r);
            }

            for (int i = 0; i < abilityOrder.Count && i < slots; i++)
            {
                var id = abilityOrder[i];
                var def = GetDef(id);
                if (def == null) continue;

                var icon = AbilityCatalog.GetIcon(def);
                var r = new Rectangle(x + i * (size + spacing) + 6, y + 6, size - 12, size - 12);
                if (icon != null) g.DrawImage(icon, r);

                int lvl = abilityLevels.TryGetValue(id, out var lv) ? lv : 0;
                using var br = new SolidBrush(Color.White);
                using var f = new Font("Segoe UI", Math.Max(10f, size * 0.18f), FontStyle.Bold);
                var lvlText = $"Ур.{lvl}";
                var textSize = g.MeasureString(lvlText, f);
                g.DrawString(lvlText, f, br, r.Left, r.Bottom - textSize.Height + 1);
            }
        }

        private void DrawKillCounter(Graphics g)
        {
            string t = $"Убито: {killCount}";
            using var f = new Font("Segoe UI Semibold", 14f);
            var size = g.MeasureString(t, f);
            int x = ClientSize.Width - (int)size.Width - 12;
            int y = 8;

            using var bg = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
            g.FillRectangle(bg, x - 6, y - 2, (int)size.Width + 12, (int)size.Height + 4);

            using var sh = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            g.DrawString(t, f, sh, x + 1, y + 1);
            using var br = new SolidBrush(Color.White);
            g.DrawString(t, f, br, x, y);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
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