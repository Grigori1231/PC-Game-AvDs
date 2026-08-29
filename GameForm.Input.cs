using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rogalik
{
    public partial class GameForm
    {
        private bool cursorHidden = false;

        private void ShowCursor()
        {
            if (cursorHidden)
            {
                try { Cursor.Show(); } catch { }
                cursorHidden = false;
            }
        }

        private void HideCursorIfFullscreen()
        {
            if (isFullscreen && !cursorHidden)
            {
                try { Cursor.Hide(); } catch { }
                cursorHidden = true;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) keyW = true;
            if (e.KeyCode == Keys.S) keyS = true;
            if (e.KeyCode == Keys.A) keyA = true;
            if (e.KeyCode == Keys.D) keyD = true;

            if (e.KeyCode == Keys.F11 || (e.Alt && e.KeyCode == Keys.Enter))
            {
                if (isFullscreen) { ExitFullscreen(); ShowCursor(); }
                else { EnterFullscreen(); if (!(overlayPause?.Visible ?? false)) HideCursorIfFullscreen(); }
                if (overlayPause?.Visible == true) CenterPauseLayout();
                return;
            }

            // Пауза
            if (e.KeyCode == Keys.Escape)
            {
                if (overlayGameOver?.Visible == true) return;
                if (picker != null && picker.IsOpen) return;
                if (introRunning) return;
                TogglePauseOverlay();
                return;
            }

            if (overlayGameOver?.Visible == true) return;
            if (picker != null && picker.IsOpen) return;
            if (overlayPause?.Visible == true) return;
            if (introRunning) return;

            if (e.KeyCode == Keys.Space) SpawnCommonEnemy();
            if (e.KeyCode == Keys.Delete) enemies.Clear();
            if (e.KeyCode == Keys.M) SpawnMageEnemy();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) keyW = false;
            if (e.KeyCode == Keys.S) keyS = false;
            if (e.KeyCode == Keys.A) keyA = false;
            if (e.KeyCode == Keys.D) keyD = false;
        }

        private void ResetMovementKeys()
        {
            keyW = keyA = keyS = keyD = false;
        }

        private void EnterFullscreen()
        {
            if (isFullscreen) return;

            windowedBounds = Bounds;
            windowedBorder = FormBorderStyle;
            windowedState = WindowState;

            StartPosition = FormStartPosition.Manual;
            var screen = Screen.FromControl(this);
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = screen.Bounds;

            isFullscreen = true;
        }

        private void ExitFullscreen()
        {
            if (!isFullscreen) return;

            FormBorderStyle = windowedBorder;
            WindowState = windowedState;
            Bounds = windowedBounds;

            isFullscreen = false;
        }

        private void UpdatePlayer(int dt)
        {
            if (introRunning) return;
            if (picker != null && picker.IsOpen) return;
            if (overlayPause != null && overlayPause.Visible) return;

            float dx = 0, dy = 0;
            if (keyW) dy -= 1;
            if (keyS) dy += 1;
            if (keyA) dx -= 1;
            if (keyD) dx += 1;

            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            bool moving = len > 0.0001f;
            if (moving) { dx /= len; dy /= len; }

            float speed = player.BaseSpeed * player.SpeedMult;
            float move = (float)(speed * dt / 16.0f);

            float newX = player.X + dx * move;
            float newY = player.Y + dy * move;
            newX = Math.Max(0, Math.Min(ClientSize.Width - player.W, newX));
            newY = Math.Max(0, Math.Min(ClientSize.Height - player.H, newY));

            float mvx = newX - player.X, mvy = newY - player.Y;
            player.X = newX;
            player.Y = newY;

            if (moving)
            {
                if (Math.Abs(mvx) > Math.Abs(mvy))
                    player.Facing = mvx > 0 ? Facing.Right : Facing.Left;
                else
                    player.Facing = mvy > 0 ? Facing.Down : Facing.Up;
            }
            player.Moving = moving;
        }
    }
}