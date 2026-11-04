using System;

namespace HalfGripSlapPack
{
    internal sealed class WindshieldCaptureClock
    {
        private readonly double interval;
        private double next;
        private bool active;
        internal WindshieldCaptureClock(float hz) { interval = 1 / Math.Max(5, Math.Min(30, hz)); }
        internal bool Due(double now, bool visibleWet)
        {
            if (!visibleWet) { active = false; return false; }
            if (active && now < next) return false;
            active = true; next = now + interval;
            return true; // One capture only, even after a hitch.
        }
    }

    internal static class WindshieldRefractionMath
    {
        internal static PanePoint Offset(float water, float gradientX, float gradientY, float rippleX, float rippleY, float strength)
        {
            float wet = WindshieldWeatherState.Unit(water);
            // Ripples survive saturation; a flat saturated height field alone has
            // zero gradient and would lose all distortion precisely when most wet.
            float x = wet * strength * (gradientX * 1.5f + rippleX * 0.55f);
            float y = wet * strength * (gradientY * 1.5f + rippleY * 0.55f);
            return new PanePoint(Math.Max(-0.02f, Math.Min(0.02f, x)), Math.Max(-0.02f, Math.Min(0.02f, y)));
        }

        internal static int CaptureHeight(int width, float aspect) => Math.Max(64, (int)Math.Round(width / Math.Max(0.25f, aspect)));
    }
}
