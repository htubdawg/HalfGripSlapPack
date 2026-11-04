using System;
using HalfGripSlapPack;

internal static class WiperKnobTests
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Equal(float expected, float actual) => Assert(Math.Abs(expected - actual) < 0.0001f && !float.IsNaN(actual), "Unexpected dial/spray value.");

    internal static void Run(Action<string, Action> check)
    {
        check("Clicking and releasing the speed knob without dragging changes nothing", () =>
        {
            foreach (WiperSpeed speed in new[] { WiperSpeed.Off, WiperSpeed.Slow, WiperSpeed.Medium, WiperSpeed.Fast })
            {
                var knob = new WiperKnobRotation(); knob.Begin(speed); knob.Move(0); knob.Release();
                Assert(knob.Setting == speed, "A stationary click advanced the dial."); Equal((int)speed, knob.Position);
            }
        });
        check("Dragging up advances through speed detents and dragging down reverses them", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off);
            knob.Move(0.25f); Assert(knob.Setting == WiperSpeed.Slow, "Up did not select slow.");
            knob.Move(0.4f); Assert(knob.Setting == WiperSpeed.Medium, "Up did not select medium.");
            knob.Move(0.4f); Assert(knob.Setting == WiperSpeed.Fast, "Up did not select fast.");
            knob.Move(-0.4f); Assert(knob.Setting == WiperSpeed.Medium, "Down did not select medium.");
            knob.Move(-0.4f); Assert(knob.Setting == WiperSpeed.Slow, "Down did not select slow.");
            knob.Move(-0.4f); Assert(knob.Setting == WiperSpeed.Off, "Down did not select off.");
        });
        check("Holding the knob still never repeats a speed change", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off); knob.Move(0.25f);
            for (int frame = 0; frame < 300; frame++) knob.Move(0);
            Assert(knob.Setting == WiperSpeed.Slow, "A hold auto-cycled settings.");
        });
        check("Dial detents resist hand jitter at their boundary", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off); knob.Move(0.244f);
            Assert(knob.Setting == WiperSpeed.Slow, "Detent did not engage.");
            knob.Move(-0.04f); knob.Move(0.02f);
            Assert(knob.Setting == WiperSpeed.Slow, "Dial chattered around the detent.");
            knob.Move(-0.08f); Assert(knob.Setting == WiperSpeed.Off, "Reverse detent did not engage.");
        });
        check("Knob stops at off and fast and reverses immediately after overshoot", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off); knob.Move(100);
            Equal(3, knob.Position); Assert(knob.Setting == WiperSpeed.Fast, "Fast wrapped to off.");
            knob.Move(-0.25f); Assert(knob.Setting == WiperSpeed.Medium, "Overshoot delayed reversal at fast.");
            knob.Move(-100); Equal(0, knob.Position);
            knob.Move(0.25f); Assert(knob.Setting == WiperSpeed.Slow, "Overshoot delayed reversal at off.");
        });
        check("Sleeve stays on a detent throughout the grab and release", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off); knob.Move(0.32f);
            knob.Release(); Equal(1, knob.Position); knob.Begin(knob.Setting); knob.Move(0.1f);
            Equal(1, knob.Position); Assert(knob.Setting == WiperSpeed.Slow, "Re-grab reused stale hand travel.");
        });
        check("Invalid knob input cannot corrupt speed or position", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Medium);
            knob.Move(float.NaN); knob.Move(float.PositiveInfinity); Equal(2, knob.Position);
            var state = new WiperState(); bool rejected = false;
            try { state.SetSpeed((WiperSpeed)4); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected && state.Speed == WiperSpeed.Off, "Invalid dial setting accepted.");
        });
        check("Rotating speed preserves the current wipe phase", () =>
        {
            var state = new WiperState(); state.SetSpeed(WiperSpeed.Slow); state.Tick(0.35f);
            var knob = new WiperKnobRotation(); knob.Begin(state.Speed); knob.Move(0.3f);
            state.SetSpeed(knob.Setting); Equal(0.5f, state.Sweep);
            Assert(!state.Washing && state.Speed == WiperSpeed.Medium, "Speed rotation engaged the washer.");
        });
        check("Passenger washer spray keeps its previous nozzle and fan trajectory", () =>
        {
            var pivot = new PanePoint(0.27f, -0.7f);
            for (int step = 0; step <= 20; step++)
                for (int drop = 0; drop < 6; drop++)
                {
                    float t = step / 20f;
                    PanePoint p = WasherSpray.Point(pivot, 0.9f, 2.47f, false, drop, t);
                    Equal(pivot.X + (drop % 3 - 1) * 2.47f * 0.06f * t, p.X);
                    Equal(pivot.Y + 0.9f * (1.8f * t - t * t), p.Y);
                }
        });
        check("Driver washer spray moves inward without changing its height or fan width", () =>
        {
            var pivot = new PanePoint(-0.815f, -0.7f);
            for (int step = 0; step <= 20; step++)
                for (int drop = 0; drop < 6; drop++)
                {
                    float t = step / 20f;
                    PanePoint old = WasherSpray.Point(pivot, 0.98f, 2.47f, false, drop, t);
                    PanePoint moved = WasherSpray.Point(pivot, 0.98f, 2.47f, true, drop, t);
                    Equal(2.47f * (0.11f + 0.04f * t), moved.X - old.X); Equal(old.Y, moved.Y);
                    Assert(moved.X > -0.7f && moved.X < 0, "Driver spray left its intended inner-left glass area.");
                }
        });
        check("Dial has exactly four discrete positions while dragging in either direction", () =>
        {
            var knob = new WiperKnobRotation(); knob.Begin(WiperSpeed.Off);
            var visited = new System.Collections.Generic.HashSet<float>();
            foreach (float delta in new[] { 0.01f, -0.01f })
                for (int frame = 0; frame < 160; frame++)
                {
                    knob.Move(delta); visited.Add(knob.Position);
                    Equal((int)knob.Setting, knob.Position);
                }
            Assert(visited.SetEquals(new[] { 0f, 1f, 2f, 3f }), "Dial exposed an intermediate position or skipped a detent.");
        });
    }
}
