using System;

namespace HalfGripSlapPack
{
    internal sealed class WiperKnobRotation
    {
        internal float Position { get; private set; }
        internal WiperSpeed Setting { get; private set; }
        internal void Begin(WiperSpeed setting) { Setting = setting; Position = (int)setting; }
        internal void Move(float upwardMovement)
        {
            if (float.IsNaN(upwardMovement) || float.IsInfinity(upwardMovement)) return;
            // 0.4 cockpit units per detent. Consume movement at both hard stops so
            // reversal is immediate, and use hysteresis to prevent detent chatter.
            Position = Math.Max(0, Math.Min(3, Position + upwardMovement * 2.5f));
            int setting = (int)Setting;
            while (setting < 3 && Position >= setting + 0.6f) setting++;
            while (setting > 0 && Position <= setting - 0.6f) setting--;
            Setting = (WiperSpeed)setting;
        }
        internal void Release() { Position = (int)Setting; }
    }

    internal static class WasherSpray
    {
        internal static PanePoint Point(PanePoint pivot, float radius, float width, bool driver, int drop, float phase)
        {
            float inward = driver ? width * (0.11f + 0.04f * phase) : 0;
            return new PanePoint(pivot.X + inward + (drop % 3 - 1) * width * 0.06f * phase,
                pivot.Y + radius * (1.8f * phase - phase * phase));
        }
    }
}
