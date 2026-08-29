using System;

namespace Rogalik
{
    public partial class GameForm
    {
        private static float FacingToAngle(Facing f) => f switch
        {
            Facing.Right => 0,
            Facing.Down => 90,
            Facing.Left => 180,
            Facing.Up => 270,
            _ => 0
        };

        private static float NormalizeAngle(float a)
        {
            while (a <= -180) a += 360;
            while (a > 180) a -= 360;
            return a;
        }

        private static double Dist2(float x1, float y1, float x2, float y2) =>
            (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);

        private static bool InsideCone(float cx, float cy, float dirDeg, float arcDeg, float radius, float x, float y)
        {
            float dx = x - cx, dy = y - cy;
            float d2 = dx * dx + dy * dy;
            if (d2 > radius * radius) return false;

            float ang = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
            float d = NormalizeAngle(ang - dirDeg);
            return Math.Abs(d) <= arcDeg / 2f;
        }
    }
}