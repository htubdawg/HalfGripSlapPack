using System;
using HalfGripSlapPack;

internal static class WiperIntermittentTests
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Equal(float expected, float actual) => Assert(Math.Abs(expected - actual) < 0.0001f, "Unexpected intermittent blade position.");

    internal static void Run(Action<string, Action> check)
    {
        check("Intermittent starts immediately with the same motor speed as continuous", () =>
        {
            var intermittent = new WiperState(); intermittent.SetSpeed(WiperSpeed.Slow);
            var continuous = new WiperState(); continuous.SetSpeed(WiperSpeed.Medium);
            for (int quarter = 0; quarter < 4; quarter++)
            {
                intermittent.Tick(0.35f); continuous.Tick(0.35f);
                Equal(continuous.Sweep, intermittent.Sweep);
            }
            Assert(intermittent.Parked, "Intermittent did not stop at the bottom.");
        });
        check("Intermittent pauses motionless at park between full wipes", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Slow); state.Tick(1);
            for (int frame = 0; frame < 7; frame++)
            {
                state.Tick(0.25f); Equal(0, state.Sweep); Assert(state.Parked, "Blades crept during the pause.");
            }
            state.Tick(0.5f); Equal(0.5f, state.Sweep);
        });
        check("Continuous mode repeats without the intermittent delay", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Medium);
            for (int cycle = 0; cycle < 5; cycle++)
            {
                state.Tick(0.5f); Equal(1, state.Sweep); state.Tick(0.5f); Equal(0, state.Sweep);
            }
            state.Tick(0.25f); Equal(0.5f, state.Sweep);
        });
        check("Selecting continuous or fast interrupts an intermittent pause immediately", () =>
        {
            foreach (WiperSpeed speed in new[] { WiperSpeed.Medium, WiperSpeed.Fast })
            {
                var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Slow); state.Tick(1.25f);
                state.SetSpeed(speed); state.Tick(speed == WiperSpeed.Fast ? 0.125f : 0.25f);
                Equal(0.5f, state.Sweep); Assert(!state.Parked, "Old pause delayed the selected continuous mode.");
            }
        });
        check("Switching off during an intermittent pause stays parked", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Slow); state.Tick(1.25f);
            state.SetSpeed(WiperSpeed.Off); state.Tick(10); Equal(0, state.Sweep);
            Assert(state.Parked, "Off started an extra intermittent wipe.");
        });
        check("Washing interrupts the intermittent pause and runs three continuous cycles", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Slow); state.Tick(1.25f);
            state.SetPull(1); state.Release();
            for (int cycle = 0; cycle < 3; cycle++)
            {
                state.Tick(0.5f); Equal(1, state.Sweep); state.Tick(0.5f); Equal(0, state.Sweep);
            }
            state.Tick(1); Equal(0, state.Sweep); Assert(state.Parked, "Intermittent pause did not resume after washing.");
            state.Tick(1.5f); Equal(1, state.Sweep);
        });
        check("Changing from fast to intermittent finishes the stroke before pausing", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Fast); state.Tick(0.125f);
            float before = state.Sweep; state.SetSpeed(WiperSpeed.Slow); Equal(before, state.Sweep);
            state.Tick(0.75f); Equal(0, state.Sweep); Assert(state.Parked, "Intermittent did not park after the stroke.");
            state.Tick(1); Equal(0, state.Sweep);
        });
        check("Releasing a long washer hold exactly at park resumes the intermittent pause", () =>
        {
            var state = new WiperState(2, 1, 0.5f); state.SetSpeed(WiperSpeed.Slow);
            state.SetPull(1); state.Tick(4); state.Release();
            state.Tick(1); Equal(0, state.Sweep); Assert(state.Parked, "Release at park started an extra wipe.");
            state.Tick(1.5f); Equal(1, state.Sweep);
        });
        check("Large frame gaps match small steps through repeated intermittent cycles", () =>
        {
            foreach (float elapsed in new[] { 0.5f, 1f, 2.5f, 3.5f, 30.5f, 301f })
            {
                var bulk = new WiperState(2, 1, 0.5f); bulk.SetSpeed(WiperSpeed.Slow);
                var steps = new WiperState(2, 1, 0.5f); steps.SetSpeed(WiperSpeed.Slow);
                bulk.Tick(elapsed);
                for (int i = 0; i < elapsed * 4; i++) steps.Tick(0.25f);
                Equal(steps.Sweep, bulk.Sweep); Assert(steps.Parked == bulk.Parked, "Large frame skipped a pause.");
            }
        });
    }
}
