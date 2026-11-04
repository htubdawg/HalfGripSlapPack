using System;
using System.Linq;
using HalfGripSlapPack;

internal static class WindshieldWeatherTests
{
    private static readonly PanePoint[] Opening = { new PanePoint(-1, 0), new PanePoint(1, 0), new PanePoint(0.85f, 0.9f), new PanePoint(-0.85f, 0.9f) };
    private static void Assert(bool pass, string message) { if (!pass) throw new Exception(message); }

    private static WindshieldWeatherState Simulate(WiperSpeed mode, float rain, float snow, float speed, int fps = 60, float duration = 20)
    {
        var field = new WindshieldWeatherState(Opening);
        var motor = new WiperState(); motor.SetSpeed(mode);
        var clock = new WindshieldWeatherClock();
        for (int i = 0; i < fps * duration; i++)
        {
            int steps = (int)Math.Ceiling(120f / fps);
            for (int s = 0; s < steps; s++)
            {
                float previous = motor.Sweep; motor.Tick(1f / fps / steps);
                clock.Record(1f / fps / steps, previous, motor.Sweep, false);
            }
            if (clock.TryStep(out float dt, out _, out float from, out float to))
            {
                field.Deposit(dt, rain, snow, speed, 0, 0, false);
                field.Wipe(from, to);
            }
        }
        return field;
    }

    private static float ClearedAreaMean(WindshieldWeatherState field)
    {
        var coverage = new WindshieldWeatherState(Opening);
        for (int i = 0; i < coverage.Water.Length; i++) coverage.Water[i] = 1;
        coverage.Wipe(0, 1);
        float sum = 0; int count = 0;
        for (int i = 0; i < field.Water.Length; i++)
            if (field.Inside[i] && coverage.Water[i] == 0) { sum += field.Water[i] + field.Snow[i]; count++; }
        return sum / count;
    }

    internal static void Run(Action<string, Action> check)
    {
        check("Dry weather leaves the windshield clear", () =>
        {
            var field = Simulate(WiperSpeed.Off, 0, 0, 50, duration: 1);
            Assert(field.Water.Sum() == 0 && field.Snow.Sum() == 0 && field.Impact.Sum() == 0, "Dry weather deposited precipitation.");
        });
        check("Rain and snow impacts persist as separate buildup", () =>
        {
            var field = Simulate(WiperSpeed.Off, 0.3f, 0.7f, 0, duration: 2);
            Assert(field.Water.Sum() > 0 && field.Snow.Sum() > 0 && field.Impact.Sum() > 0, "Missing rain/snow or impact layer.");
            float before = field.Snow.Sum();
            field.Deposit(0.1f, 0, 0, 0, 0, 0, false);
            Assert(field.Snow.Sum() > before * 0.99f, "Snow disappeared when weather stopped.");
            field.Deposit(0.3f, 0, 0, 0, 0, 0, false);
            Assert(field.Impact.Sum() == 0 && field.Water.Sum() > 0, "Impact flash did not fade separately from buildup.");
        });
        check("Parked blades do not wipe; rubber clears both layers only inside its swept area", () =>
        {
            var field = new WindshieldWeatherState(Opening);
            for (int i = 0; i < field.Water.Length; i++) if (field.Inside[i]) { field.Water[i] = 1; field.Snow[i] = 1; }
            float before = field.Water.Sum(); field.Wipe(0, 0);
            Assert(before == field.Water.Sum(), "Parked blades cleared glass.");
            field.Wipe(0, 0.5f);
            float half = field.Water.Sum();
            Assert(half < before && half > before * 0.5f, "Partial stroke over-cleared or did not clear.");
            field.Wipe(0.5f, 1);
            Assert(field.Water.Sum() < half && field.Water.Sum() > 0, "Full stroke must leave unswept edges.");
            Assert(field.Water.Sum() == field.Snow.Sum(), "Slush was not cleared with rain.");
        });
        check("Intensity and vehicle incidence increase deposition", () =>
        {
            var light = Simulate(WiperSpeed.Off, 0.2f, 0, 0, duration: 2);
            var heavy = Simulate(WiperSpeed.Off, 1, 0, 0, duration: 2);
            var moving = Simulate(WiperSpeed.Off, 1, 0, 50, duration: 2);
            Assert(heavy.Water.Sum() > light.Water.Sum() * 3, "Intensity has insufficient effect.");
            Assert(moving.Water.Sum() > heavy.Water.Sum() * 1.5f, "Car incidence did not add impacts.");
        });
        check("Relative airflow advects water in either direction while slush moves more slowly", () =>
        {
            foreach (float direction in new[] { -1f, 1f })
            {
                var field = new WindshieldWeatherState(Opening);
                int x = 80, y = 40, center = y * WindshieldWeatherState.Width + x;
                field.Water[center] = field.Snow[center] = 1;
                float flow = direction * field.SpanX / WindshieldWeatherState.Width;
                field.Deposit(1, 0, 0, 0, flow, 0, false);
                Assert(field.Water[center + (int)direction] > 0.9f, "Water moved against airflow.");
                Assert(field.Snow[center] > 0.9f, "Slush moved as quickly as water.");
            }
        });
        check("Continuous mode keeps vanilla precipitation clear and fast mode handles heavier rain", () =>
        {
            var off = Simulate(WiperSpeed.Off, 0.5f, 0, 0);
            var slow = Simulate(WiperSpeed.Slow, 0.5f, 0, 0);
            var normal = Simulate(WiperSpeed.Medium, 0.5f, 0, 0);
            var fast = Simulate(WiperSpeed.Fast, 1, 0, 0);
            float a = ClearedAreaMean(off), b = ClearedAreaMean(slow), c = ClearedAreaMean(normal), d = ClearedAreaMean(fast);
            Console.WriteLine("Buildup in swept area (vanilla off/intermittent/continuous; heavy fast): " + a + " / " + b + " / " + c + " / " + d);
            Assert(b < a * 0.3f && c < b, "Selected modes do not progressively reduce buildup.");
            Assert(WindshieldWeatherOptics.Opacity(c, 0) < 0.16f && WindshieldWeatherOptics.Opacity(d, 0) < 0.16f, "Continuous/fast failed visibility target.");
        });
        check("Washer deposits fluid in dry weather without inventing snow", () =>
        {
            var field = new WindshieldWeatherState(Opening);
            field.Deposit(0.2f, 0, 0, 0, 0, 0, true);
            Assert(field.Water.Sum() > 0 && field.Snow.Sum() == 0, "Washer fluid missing or snowy.");
        });
        check("Fast reduces average heavy-weather buildup compared with continuous", () =>
        {
            var coverage = new WindshieldWeatherState(Opening);
            for (int i = 0; i < coverage.Water.Length; i++) coverage.Water[i] = 1;
            coverage.Wipe(0, 1);
            int[] swept = Enumerable.Range(0, coverage.Water.Length).Where(i => coverage.Inside[i] && coverage.Water[i] == 0).ToArray();
            float[] averages = new float[2];
            for (int mode = 0; mode < 2; mode++)
            {
                var field = new WindshieldWeatherState(Opening);
                var motor = new WiperState(); motor.SetSpeed(mode == 0 ? WiperSpeed.Medium : WiperSpeed.Fast);
                for (int frame = 0; frame < 1200; frame++)
                {
                    float previous = motor.Sweep; motor.Tick(1f / 60);
                    field.Deposit(1f / 60, 0.6f, 0.4f, 35, 0, 0, false);
                    field.Wipe(previous, motor.Sweep);
                    if (frame >= 600 && frame % 6 == 0)
                        averages[mode] += swept.Sum(i => field.Water[i] + field.Snow[i]) / swept.Length / 100;
                }
            }
            Assert(averages[1] < averages[0] * 0.8f, "Fast mode did not improve sustained heavy precipitation: " + averages[0] + " / " + averages[1]);
        });
        check("Frame rate changes preserve comparable buildup and clearing", () =>
        {
            float low = ClearedAreaMean(Simulate(WiperSpeed.Fast, 0.5f, 0.5f, 40, 30, 8));
            float high = ClearedAreaMean(Simulate(WiperSpeed.Fast, 0.5f, 0.5f, 40, 120, 8));
            Assert(Math.Abs(low - high) < 0.005f, "Buildup depends too strongly on frame rate: " + low + " vs " + high);
        });
        check("Invalid precipitation and airflow inputs cannot poison later simulation", () =>
        {
            var field = new WindshieldWeatherState(Opening);
            field.Deposit(float.NaN, 1, 1, 0, 0, 0, false);
            field.Deposit(0.1f, float.NaN, float.PositiveInfinity, float.NaN, float.PositiveInfinity, float.NaN, false);
            field.Deposit(0.1f, 1, 0, 0, 0, 0, false);
            Assert(field.Water.Sum() > 0 && !float.IsNaN(field.Water.Sum()), "Invalid inputs broke subsequent deposits.");
        });
        check("Buildup stays finite, inside the opening and within capacity", () =>
        {
            var field = Simulate(WiperSpeed.Off, 1, 1, 100, duration: 3);
            for (int i = 0; i < field.Water.Length; i++)
            {
                Assert(field.Water[i] >= 0 && field.Water[i] <= 1 && field.Snow[i] >= 0 && field.Snow[i] <= 1, "Invalid field value.");
                Assert(field.Inside[i] || field.Water[i] + field.Snow[i] == 0, "Precipitation outside glass.");
            }
        });
        check("Weather field updates are bounded independently of rendering frame rate", () =>
        {
            foreach (int fps in new[] { 30, 60, 144, 240 })
            {
                var clock = new WindshieldWeatherClock(); int ticks = 0;
                for (int i = 0; i < fps * 10; i++)
                {
                    clock.Record(1f / fps, 0, 0, false);
                    if (clock.TryStep(out _, out _, out _, out _)) ticks++;
                }
                Assert(ticks >= 199 && ticks <= 200, "Weather ran at render rate: " + fps + "fps -> " + ticks + " ticks.");
            }
        });
        check("Batched clearing retains reversals, consumes each path once and preserves washer duration", () =>
        {
            var clock = new WindshieldWeatherClock();
            clock.Record(0.02f, 0.7f, 1, true);
            clock.Record(0.02f, 1, 0.9f, false);
            clock.Record(0.02f, 0.9f, 0.8f, false);
            Assert(clock.TryStep(out float dt, out float wash, out float from, out float to), "Field step was not due.");
            Assert(Math.Abs(dt - 0.06f) < 0.0001f && Math.Abs(wash - 1f / 3) < 0.0001f, "Washer duration changed.");
            Assert(from == 0.7f && to == 1, "Stroke reversal was missed.");
            Assert(!clock.TryStep(out _, out _, out _, out _), "Field step ran twice.");
            clock.Record(0.1f, 0, 0, false);
            Assert(clock.TryStep(out _, out _, out from, out to) && from == to, "Old wipe path was replayed while parked.");
            clock.Record(5, 0, 1, false);
            Assert(clock.TryStep(out dt, out _, out _, out _) && dt <= 0.25f, "Hitch created catch-up work.");
            Assert(!clock.TryStep(out _, out _, out _, out _), "Hitch left a catch-up queue.");
        });
        check("No-wiper rain develops substantial obscuration and the setting changes its strength", () =>
        {
            var field = Simulate(WiperSpeed.Off, 0.5f, 0, 0, 144, 10);
            float alpha = 0; int count = 0;
            for (int i = 0; i < field.Water.Length; i++)
                if (field.Inside[i]) { alpha += WindshieldWeatherOptics.Opacity(field.Water[i], 0); count++; }
            alpha /= count;
            Console.WriteLine("Vanilla rain, 10s with wipers off: mean opacity=" + alpha);
            Assert(alpha > 0.6f, "Wipers-off rain remained too transparent: " + alpha);
            Assert(WindshieldWeatherOptics.Opacity(0, 0) == 0, "Dry glass is obscured.");
            Assert(WindshieldWeatherOptics.Opacity(0.2f, 0, 2) > WindshieldWeatherOptics.Opacity(0.2f, 0, 1), "Obscuration setting has no effect.");
        });
    }
}
