using System;

namespace HalfGripSlapPack
{
    internal enum WiperSpeed { Off, Slow, Medium, Fast }

    // Phase always advances through a complete out-and-back stroke. Stopping never
    // reverses or teleports a moving blade; the current cycle finishes at the park stop.
    internal sealed class WiperState
    {
        private readonly double[] periods;
        private double phase;
        private bool cycling;
        internal WiperSpeed Speed { get; private set; }
        internal float Pull { get; private set; }
        internal bool Washing => Pull >= 0.3f;
        internal bool Parked => !cycling;
        internal float Sweep => (float)((1 - Math.Cos(phase * Math.PI * 2)) * 0.5);

        internal WiperState(float slow = 2.4f, float medium = 1.4f, float fast = 0.8f)
        {
            if (!Positive(slow) || !Positive(medium) || !Positive(fast) || slow <= medium || medium <= fast)
                throw new ArgumentOutOfRangeException(nameof(slow), "Wipe periods must be finite, positive and decrease with speed.");
            periods = new[] { 0.0, slow, medium, fast };
        }

        internal void NextSpeed() { Speed = (WiperSpeed)(((int)Speed + 1) % 4); }
        internal void SetPull(float fraction)
        {
            if (float.IsNaN(fraction) || float.IsInfinity(fraction)) return;
            Pull = Math.Max(0, Math.Min(1, fraction));
        }
        internal void Release() { Pull = 0; }

        internal void Tick(float seconds)
        {
            if (!Positive(seconds)) return;
            int effective = Washing ? Math.Max(2, (int)Speed) : (int)Speed;
            if (effective == 0 && phase == 0) { cycling = false; return; }
            if (effective == 0 && !cycling) return;
            if (effective > 0) cycling = true;
            // Use the last operating period while completing an off-commanded cycle.
            if (effective > 0) lastPeriod = periods[effective];
            phase += seconds / lastPeriod;
            if (phase < 1) return;
            if (effective == 0) { phase = 0; cycling = false; }
            else phase %= 1;
        }

        private double lastPeriod = 1.4;
        private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
