using System;
using System.Collections.Generic;
using HalfGripSlapPack;

internal static class WiperTests
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Equal(float expected, float actual)
    {
        Assert(Math.Abs(expected - actual) < 0.0001f && !float.IsNaN(actual), "Expected " + expected + ", got " + actual);
    }

    internal static void Run(Action<string, Action> check)
    {
        check("Wipers start off, dry and parked", () =>
        {
            var state = new WiperState(); state.Tick(20);
            Assert(state.Speed == WiperSpeed.Off && state.Parked && !state.Washing, "Unexpected startup state.");
            Equal(0, state.Sweep);
        });
        check("Wiper dial cycles off slow medium fast off", () =>
        {
            var state = new WiperState();
            foreach (WiperSpeed expected in new[] { WiperSpeed.Slow, WiperSpeed.Medium, WiperSpeed.Fast, WiperSpeed.Off })
            {
                state.NextSpeed(); Assert(state.Speed == expected, "Dial order changed.");
            }
        });
        check("Each speed makes one smooth out-and-back sweep in its period", () =>
        {
            foreach (int speed in new[] { 1, 2, 3 })
            {
                var state = new WiperState(); for (int n = 0; n < speed; n++) state.NextSpeed();
                float period = new[] { 0f, 2.4f, 1.4f, 0.8f }[speed];
                state.Tick(period / 4); Equal(0.5f, state.Sweep);
                state.Tick(period / 4); Equal(1, state.Sweep);
                state.Tick(period / 4); Equal(0.5f, state.Sweep);
                state.Tick(period / 4); Equal(0, state.Sweep);
            }
        });
        check("Full pull washes continuously while held with the dial off", () =>
        {
            var state = new WiperState(); state.SetPull(1); state.Tick(1.4f * 4 + 0.35f);
            Assert(state.Washing && !state.Parked && state.Speed == WiperSpeed.Off, "Wash changed the dial.");
            Equal(0.5f, state.Sweep);
        });
        check("Wash detent ignores tiny downward hand movement", () =>
        {
            var state = new WiperState(); state.SetPull(0.29f); state.Tick(1);
            Assert(!state.Washing && state.Parked, "Washer started on initial grab.");
            state.SetPull(0.3f); state.Tick(0.1f); Assert(state.Washing && !state.Parked, "Wash detent did not engage.");
        });
        check("Releasing wash immediately stops spray and springs the stalk back", () =>
        {
            var state = new WiperState(); state.SetPull(1); state.Tick(0.3f);
            float before = state.Sweep; state.Release();
            Equal(0, state.Pull); Equal(before, state.Sweep);
            Assert(!state.Washing, "Spray remained active after release.");
            state.Tick(2); Assert(state.Parked, "Off wipers did not park after release."); Equal(0, state.Sweep);
        });
        check("Washer release resumes the selected speed without moving the dial", () =>
        {
            var state = new WiperState(); state.NextSpeed(); state.SetPull(1); state.Tick(0.7f);
            Equal(1, state.Sweep); state.Release(); state.Tick(0.6f);
            Equal(0.5f, state.Sweep); Assert(state.Speed == WiperSpeed.Slow && !state.Parked, "Slow setting was lost.");
        });
        check("Wash never slows fast wipers down", () =>
        {
            var state = new WiperState(); for (int i = 0; i < 3; i++) state.NextSpeed();
            state.SetPull(1); state.Tick(0.4f); Equal(1, state.Sweep);
        });
        check("Switching off mid-stroke completes the current cycle before parking", () =>
        {
            foreach (float elapsed in new[] { 0.2f, 0.6f })
            {
                var state = new WiperState(); for (int i = 0; i < 3; i++) state.NextSpeed();
                state.Tick(elapsed); float before = state.Sweep; state.NextSpeed(); Equal(before, state.Sweep);
                state.Tick(0.01f); Assert(!state.Parked, "Off command teleported the blades.");
                state.Tick(1); Assert(state.Parked, "Wipers did not finish parking."); Equal(0, state.Sweep);
                state.Tick(1); Equal(0, state.Sweep);
            }
        });
        check("Switching off exactly at park does not start another stroke", () =>
        {
            var state = new WiperState(); for (int i = 0; i < 3; i++) state.NextSpeed();
            state.Tick(0.8f); state.NextSpeed(); state.Tick(0.1f);
            Assert(state.Parked, "An extra cycle started at the park stop."); Equal(0, state.Sweep);
        });
        check("Speed changes preserve blade position and phase", () =>
        {
            var state = new WiperState(); state.NextSpeed(); state.Tick(0.6f);
            state.NextSpeed(); Equal(0.5f, state.Sweep);
            state.Tick(0.35f); Equal(1, state.Sweep);
        });
        check("Wiper pause and invalid inputs leave animation stable", () =>
        {
            var state = new WiperState(); state.SetPull(1); state.Tick(0.35f);
            foreach (float dt in new[] { 0, -1, float.NaN, float.PositiveInfinity }) state.Tick(dt);
            state.SetPull(float.NaN); state.SetPull(float.PositiveInfinity);
            Equal(0.5f, state.Sweep); Equal(1, state.Pull);
            state.SetPull(-1); Equal(0, state.Pull);
        });
        check("Wiper timing rejects invalid or unordered speed periods", () =>
        {
            foreach (float[] periods in new[] { new[] { 0f, 1f, 0.5f }, new[] { 1f, 2f, 0.5f }, new[] { float.NaN, 1f, 0.5f } })
            {
                bool threw = false;
                try { new WiperState(periods[0], periods[1], periods[2]); } catch (ArgumentOutOfRangeException) { threw = true; }
                Assert(threw, "Invalid timing accepted.");
            }
        });
        check("Both wiper sweeps clear a trapezoidal windshield throughout travel", () =>
        {
            var opening = WindowPolygon.Hull(new[] { new PanePoint(-1.24f, 0), new PanePoint(1.24f, 0),
                new PanePoint(0.98f, 1.5f), new PanePoint(-0.98f, 1.5f) });
            CheckSweep(opening);
        });
        check("Wiper sweeps fit the stock windshield's raised center and sloped lower edge", () =>
        {
            // The supplied stock front glass projected into its own plane, in car units.
            var opening = WindowPolygon.Hull(new[] { new PanePoint(-1.235474f, -0.791847f), new PanePoint(0, -0.720308f),
                new PanePoint(1.235474f, -0.791847f), new PanePoint(0.982076f, 0.742094f), new PanePoint(0, 0.770062f), new PanePoint(-0.982076f, 0.742094f) });
            CheckSweep(opening);
        });
        check("Wiper knob settings remain distinct at native pixel resolution", () =>
        {
            var hashes = new HashSet<string>();
            for (int speed = 0; speed < 4; speed++)
            {
                byte[] pixels = WiperStalkPixels.Build(speed);
                Assert(pixels.Length == 128 * 20, "Wiper art resolution changed.");
                foreach (byte pixel in pixels) Assert(pixel < SignalStalkPixels.Palette.Length, "Unknown palette color.");
                hashes.Add(Convert.ToBase64String(pixels));
            }
            Assert(hashes.Count == 4, "Dial setting has no visual indication.");
        });
    }

    private static void CheckSweep(List<PanePoint> opening)
    {
        foreach (bool passenger in new[] { false, true })
        {
            var geometry = new WiperGeometry(opening, passenger);
            Assert(geometry.Radius > 0.5f, "Fitted blade is too small.");
            for (int frame = 0; frame <= 200; frame++)
                foreach (float radial in new[] { 0, geometry.Radius * 0.35f, geometry.Radius })
                    foreach (float side in new[] { -0.03f, 0.03f })
                    {
                        PanePoint p = geometry.Point(radial, side, frame / 200f);
                        for (int edge = 0; edge < opening.Count; edge++)
                        {
                            PanePoint a = opening[edge], b = opening[(edge + 1) % opening.Count];
                            float cross = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
                            Assert(cross >= -0.0001f, "Blade crosses the windshield frame.");
                        }
                    }
        }
    }
}
