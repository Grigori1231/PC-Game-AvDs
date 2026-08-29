using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rogalik
{
    internal class YellowHealthBar : Control
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value0 = 100;
        public int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (maximum <= minimum) maximum = minimum + 1;
                if (value0 < minimum) value0 = minimum;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                maximum = Math.Max(minimum + 1, value);
                if (value0 > maximum) value0 = maximum;
                Invalidate();
            }
        }

        public int Value
        {
            get => value0;
            set
            {
                value0 = Math.Max(minimum, Math.Min(maximum, value));
                Invalidate();
            }
        }

        public YellowHealthBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(260, 22);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var bg = new SolidBrush(Color.FromArgb(50, 50, 56)))
                g.FillRectangle(bg, rect);

            float frac = (maximum <= minimum)
                ? 0f
                : (Value - minimum) / (float)(maximum - minimum);
            frac = Math.Max(0f, Math.Min(1f, frac));

            int fillW = (int)Math.Round((Width - 2) * frac);
            if (fillW > 0)
            {
                var fillRect = new Rectangle(1, 1, fillW, Height - 2);
                using var br = new LinearGradientBrush(fillRect,
                    Color.FromArgb(245, 200, 40),
                    Color.FromArgb(255, 240, 90),
                    LinearGradientMode.Vertical);
                g.FillRectangle(br, fillRect);

                var gloss = new Rectangle(1, 1, fillW, Math.Max(2, (Height - 2) / 2));
                using var glossBr = new LinearGradientBrush(gloss,
                    Color.FromArgb(60, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.Vertical);
                g.FillRectangle(glossBr, gloss);
            }

            using var pen = new Pen(Color.FromArgb(180, 200, 170, 50), 1.5f);
            g.DrawRectangle(pen, rect);
        }
    }

    public partial class GameForm
    {

        private Panel overlayPause;
        private Panel pauseBox;
        private MenuButton pauseBtnResume, pauseBtnMenu;
        private FlowLayoutPanel pauseBtnPanel;

        private Panel pauseStats;
        private Label lblStatHp, lblStatArmor, lblStatXp, lblStatSpeed, lblStatCritC, lblStatCritM, lblStatMulticast;

        private void BuildUI()
        {
            hpBar = new YellowHealthBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                Width = 260,
                Height = 22
            };
            Controls.Add(hpBar);
            Resize += (s, e) => RelayoutBottom();
            RelayoutBottom();

            overlayGameOver = new Panel
            {
                Visible = false,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(160, 0, 0, 0)
            };
            Controls.Add(overlayGameOver);
            overlayGameOver.BringToFront();

            gameOverBox = new Panel
            {
                Width = 520,
                Height = 280,
                BackColor = Color.FromArgb(36, 36, 44),
                BorderStyle = BorderStyle.FixedSingle
            };
            overlayGameOver.Controls.Add(gameOverBox);

            var overLbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 72,
                Text = "Игра окончена",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20, FontStyle.Bold)
            };
            gameOverBox.Controls.Add(overLbl);

            var overBtnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 96,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(20, 12, 20, 20),
                BackColor = Color.Transparent
            };
            gameOverBox.Controls.Add(overBtnPanel);

            overBtnRestart = new MenuButton
            {
                Text = "Играть снова",
                Width = 220,
                Height = 56,
                Font = new Font("Segoe UI Semibold", 14f),
                Margin = new Padding(10, 8, 10, 8)
            };
            overBtnMenu = new MenuButton
            {
                Text = "Вернуться в меню",
                Width = 220,
                Height = 56,
                Font = new Font("Segoe UI Semibold", 14f),
                Margin = new Padding(10, 8, 10, 8)
            };

            overBtnRestart.Click += (s, e) => RestartGame();

            overBtnMenu.Click += (s, e) =>
            {
                Save.HighScore = Math.Max(Save.HighScore, killCount);
                LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
                ShowCursor();
                Close();
            };

            overBtnPanel.Controls.Add(overBtnRestart);
            overBtnPanel.Controls.Add(overBtnMenu);

            overlayGameOver.VisibleChanged += (s, e) => { if (overlayGameOver.Visible) CenterGameOverBox(); };
            overlayGameOver.Resize += (s, e) => { if (overlayGameOver.Visible) CenterGameOverBox(); };

            BuildPauseOverlay();
        }

        private void BuildPauseOverlay()
        {
            overlayPause = new Panel
            {
                Visible = false,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(140, 0, 0, 0)
            };
            Controls.Add(overlayPause);
            overlayPause.BringToFront();

            pauseBox = new Panel
            {
                Width = 400,
                Height = 260,
                BackColor = Color.FromArgb(36, 36, 44),
                BorderStyle = BorderStyle.FixedSingle
            };
            overlayPause.Controls.Add(pauseBox);

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 64,
                Text = "Пауза",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };
            pauseBox.Controls.Add(lblTitle);

            pauseBtnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 170,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 18),
                BackColor = Color.Transparent
            };
            pauseBox.Controls.Add(pauseBtnPanel);

            pauseBtnResume = new MenuButton
            {
                Text = "Продолжить",
                Width = 300,
                Height = 56,
                Font = new Font("Segoe UI Semibold", 15f),
                Margin = new Padding(10)
            };
            pauseBtnMenu = new MenuButton
            {
                Text = "В меню",
                Width = 300,
                Height = 56,
                Font = new Font("Segoe UI Semibold", 15f),
                Margin = new Padding(10)
            };

            pauseBtnResume.Click += (s, e) => TogglePauseOverlay();

            pauseBtnMenu.Click += (s, e) =>
            {
                Save.HighScore = Math.Max(Save.HighScore, killCount);
                LoadingForm.ShowFullscreenFor(this, "Загрузка...", 3000);
                ShowCursor();
                Close();
            };

            pauseBtnPanel.Controls.Add(pauseBtnResume);
            pauseBtnPanel.Controls.Add(pauseBtnMenu);

            pauseStats = new Panel
            {
                Width = 260,
                Height = 260,
                BackColor = Color.FromArgb(36, 36, 44),
                BorderStyle = BorderStyle.FixedSingle
            };
            overlayPause.Controls.Add(pauseStats);

            var cap = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Статистика",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            pauseStats.Controls.Add(cap);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(8),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            pauseStats.Controls.Add(table);

            void Row(string name, out Label valueLabel)
            {
                var l = new Label
                {
                    AutoSize = true,
                    Text = name,
                    ForeColor = Color.Gainsboro,
                    Font = new Font("Segoe UI", 10),
                    Margin = new Padding(2, 6, 2, 6),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top
                };
                valueLabel = new Label
                {
                    AutoSize = true,
                    Text = "-",
                    ForeColor = Color.FromArgb(230, 200, 90),
                    Font = new Font("Segoe UI Semibold", 10),
                    Margin = new Padding(2, 6, 2, 6),
                    Anchor = AnchorStyles.Right | AnchorStyles.Top
                };
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(l);
                table.Controls.Add(valueLabel);
            }

            Row("HP:", out lblStatHp);
            Row("Броня:", out lblStatArmor);
            Row("Опыт:", out lblStatXp);
            Row("Скорость:", out lblStatSpeed);
            Row("Крит шанс:", out lblStatCritC);
            Row("Крит множ.:", out lblStatCritM);
            Row("Мультикаст:", out lblStatMulticast);

            overlayPause.VisibleChanged += (s, e) =>
            {
                if (overlayPause.Visible)
                {
                    CenterPauseLayout();
                    RecalcPauseButtonsPadding();
                }
            };
            overlayPause.Resize += (s, e) =>
            {
                if (overlayPause.Visible)
                {
                    CenterPauseLayout();
                    RecalcPauseButtonsPadding();
                }
            };
        }

        private void CenterGameOverBox()
        {
            gameOverBox.Left = (ClientSize.Width - gameOverBox.Width) / 2;
            gameOverBox.Top = (ClientSize.Height - gameOverBox.Height) / 2;
        }

        private void CenterPauseLayout()
        {
            if (pauseBox == null || pauseStats == null) return;

            int gap = 24;
            int totalW = pauseBox.Width + gap + pauseStats.Width;

            int startX = (ClientSize.Width - totalW) / 2;
            int sameTop = (ClientSize.Height - Math.Max(pauseBox.Height, pauseStats.Height)) / 2;

            pauseBox.Left = startX;
            pauseBox.Top = sameTop;

            pauseStats.Left = pauseBox.Right + gap;
            pauseStats.Top = sameTop;

            RecalcPauseButtonsPadding();
        }

        private void RecalcPauseButtonsPadding()
        {
            if (pauseBtnPanel == null || pauseBox == null || pauseBtnResume == null) return;
            int btnW = pauseBtnResume.Width;
            int hpad = Math.Max(0, (pauseBox.Width - btnW) / 2);
            pauseBtnPanel.Padding = new Padding(hpad, pauseBtnPanel.Padding.Top, hpad, pauseBtnPanel.Padding.Bottom);
        }

        private void RelayoutBottom()
        {
            if (hpBar == null) return;
            hpBar.Left = 12;
            hpBar.Top = ClientSize.Height - hpBar.Height - 12;
        }

        private void TogglePauseOverlay()
        {
            if (overlayPause.Visible)
            {
                overlayPause.Visible = false;
                HideCursorIfFullscreen();
                ResumeGame();
            }
            else
            {
                PauseGame();
                ShowCursor();
                RefreshStatsPanel();
                overlayPause.Visible = true;
                overlayPause.BringToFront();
                CenterPauseLayout();
            }
        }

        partial void RefreshStatsPanel()
        {
            if (pauseStats == null || player == null) return;

            string HpText() => $"{player.HP}/{player.MaxHP}";
            string ArmorText() => $"{(int)Math.Round(player.Armor)}";

            int xpPct = (int)Math.Round((player.XpGainMult - 1f) * 100f);
            if (xpPct < 0) xpPct = 0;

            int spPct = (int)Math.Round((player.SpeedMult - 1f) * 100f);
            if (spPct < 0) spPct = 0;

            int ccPct = (int)Math.Round(player.CritChance * 100f);
            if (ccPct < 0) ccPct = 0;

            int cmPct = (int)Math.Round((player.CritMult - 1f) * 100f);
            if (cmPct < 0) cmPct = 0;

            int multiPct = Math.Max(0, (int)Math.Round(multicastChance * 100f));

            lblStatHp.Text = HpText();
            lblStatArmor.Text = ArmorText();
            lblStatXp.Text = $"{xpPct}%";
            lblStatSpeed.Text = $"{spPct}%";
            lblStatCritC.Text = $"{ccPct}%";
            lblStatCritM.Text = $"{cmPct}%";
            lblStatMulticast.Text = $"{multiPct}%";
        }
    }
}