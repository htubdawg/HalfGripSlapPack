using System;
using System.Linq;
using HalfGripSlapPack;

internal static class SignalRelayTests
{
    private static void Require(bool condition)
    { if (!condition) throw new Exception("Unexpected signal relay behavior."); }

    internal static void Run(Action<string, Action> check)
    {
        check("Relay clicks once per on/off edge, not every frame", () =>
        {
            var relay = new SignalRelayTracker();
            Require(relay.Step(0, false, true) == RelayCue.None);
            Require(relay.Step(1, true, true) == RelayCue.Tick);
            Require(relay.Step(1, true, true) == RelayCue.None);
            Require(relay.Step(1, false, true) == RelayCue.Tock);
            Require(relay.Step(1, false, true) == RelayCue.None);
        });
        check("Manual cancellation opens the relay with a tock", () =>
        {
            var relay = new SignalRelayTracker(); relay.Step(-1, true, true);
            Require(relay.Step(0, false, true) == RelayCue.Tock);
            Require(relay.Step(0, false, true) == RelayCue.None);
        });
        check("Changing signal sides restarts the relay tick", () =>
        {
            var relay = new SignalRelayTracker(); relay.Step(-1, true, true);
            Require(relay.Step(1, true, true) == RelayCue.Tick);
        });
        check("Silent or paused phases do not replay on return to the cockpit", () =>
        {
            var relay = new SignalRelayTracker();
            Require(relay.Step(-1, true, false) == RelayCue.None);
            Require(relay.Step(-1, true, true) == RelayCue.None);
            Require(relay.Step(-1, false, false) == RelayCue.None);
            Require(relay.Step(-1, false, true) == RelayCue.None);
            Require(relay.Step(-1, true, true) == RelayCue.Tick);
        });
        check("Three short flashes produce three paired ticks and tocks", () =>
        {
            var state = new TurnSignalState(); var relay = new SignalRelayTracker();
            state.Move(0.5f); state.Release(); int ticks = 0, tocks = 0;
            for (int i = 0; i < 7; i++)
            {
                RelayCue cue = relay.Step(state.Direction, state.FlashOn, true);
                if (cue == RelayCue.Tick) ticks++; if (cue == RelayCue.Tock) tocks++;
                state.Tick(0.4f);
            }
            Require(ticks == 3 && tocks == 3 && state.Direction == 0);
        });
        check("Relay clips have short mechanical transient durations", () =>
        {
            Require(SignalRelaySamples.Generate(true).Length == (int)(SignalRelaySamples.SampleRate * 0.045));
            Require(SignalRelaySamples.Generate(false).Length == (int)(SignalRelaySamples.SampleRate * 0.055));
        });
        check("Relay samples are finite, audible, and do not clip", () =>
        {
            foreach (bool on in new[] { true, false })
            {
                float[] samples = SignalRelaySamples.Generate(on);
                Require(samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s) && Math.Abs(s) < 1));
                Require(samples.Max(s => Math.Abs(s)) > 0.1f);
            }
        });
        check("Relay transients start/end at zero and decay before the next blink", () =>
        {
            foreach (bool on in new[] { true, false })
            {
                float[] samples = SignalRelaySamples.Generate(on);
                Require(samples[0] == 0 && samples[samples.Length - 1] == 0);
                double early = samples.Take(300).Average(s => s * s);
                double tail = samples.Skip(samples.Length - 100).Average(s => s * s);
                Require(tail < early * 0.01);
            }
        });
        check("Tick and tock are distinct and generation is repeatable", () =>
        {
            float[] on = SignalRelaySamples.Generate(true), off = SignalRelaySamples.Generate(false);
            Require(on.SequenceEqual(SignalRelaySamples.Generate(true)));
            Require(!on.Take(300).SequenceEqual(off.Take(300)));
        });
    }
}
