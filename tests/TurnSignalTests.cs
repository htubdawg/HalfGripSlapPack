using System;
using HalfGripSlapPack;

internal static class TurnSignalTests
{
    private static void Require(bool condition, string message = "Unexpected turn-signal behavior")
    { if (!condition) throw new Exception(message); }

    private static TurnSignalState Latch(int direction)
    {
        var state = new TurnSignalState();
        state.Move(direction); state.Release();
        return state;
    }

    internal static void Run(Action<string, Action> check)
    {
        check("Signal starts neutral and unlit", () =>
        {
            var s = new TurnSignalState();
            Require(s.Direction == 0 && !s.FlashOn && !s.Latched && s.StalkPosition == 0);
        });
        check("Down halfway signals left and up halfway signals right", () =>
        {
            var s = new TurnSignalState(); s.Move(-0.5f);
            Require(s.Direction == -1 && s.FlashOn && !s.Latched);
            s.Move(0.5f); Require(s.Direction == 1 && s.FlashOn && !s.Latched);
        });
        check("A released halfway tap completes exactly three on phases", () =>
        {
            var s = new TurnSignalState(); s.Move(-0.5f); s.Release();
            Require(s.StalkPosition == 0);
            for (int i = 0; i < 4; i++) { s.Tick(0.4f); Require(s.Direction == -1); }
            Require(s.FlashOn); s.Tick(0.4f);
            Require(s.Direction == 0 && !s.FlashOn);
        });
        check("Halfway can be held beyond the short flash count", () =>
        {
            var s = new TurnSignalState(); s.Move(0.5f); s.Tick(8);
            Require(s.Direction == 1 && s.Held && !s.Latched && s.StalkPosition == 0.5f);
            s.Release(); Require(s.Direction == 0 && s.StalkPosition == 0);
        });
        check("Release during an early half-hold completes the remaining short flashes", () =>
        {
            var s = new TurnSignalState(); s.Move(0.5f); s.Tick(0.4f); s.Release();
            for (int i = 0; i < 3; i++) { s.Tick(0.4f); Require(s.Direction == 1); }
            s.Tick(0.4f); Require(s.Direction == 0);
        });
        check("Full travel latches either direction after release", () =>
        {
            foreach (int direction in new[] { -1, 1 })
            {
                var s = Latch(direction); s.Tick(10);
                Require(s.Latched && s.Direction == direction && s.StalkPosition == direction);
            }
        });
        check("Returning the stalk to neutral cancels a latched signal", () =>
        {
            var s = Latch(-1); s.Move(0); s.Release();
            Require(s.Direction == 0 && !s.Latched && !s.FlashOn);
        });
        check("Returning to neutral also cancels a short signal", () =>
        {
            var s = new TurnSignalState(); s.Move(0.5f); s.Release(); s.Move(0);
            Require(s.Direction == 0 && !s.FlashOn);
        });
        check("A full latch persists if pulled only partway back", () =>
        {
            var s = Latch(1); s.Move(0.5f); s.Release();
            Require(s.Latched && s.Direction == 1 && s.StalkPosition == 1);
        });
        check("Switching directions resets the short-signal burst", () =>
        {
            var s = new TurnSignalState(); s.Move(-0.5f); s.Tick(1.2f);
            s.Move(0.5f); s.Release(); Require(s.Direction == 1 && s.FlashOn);
            for (int i = 0; i < 4; i++) { s.Tick(0.4f); Require(s.Direction == 1); }
            s.Tick(0.4f); Require(s.Direction == 0);
        });
        check("Repeated held input does not restart the blink phase", () =>
        {
            var s = new TurnSignalState(); s.Move(-0.5f); s.Tick(0.3f);
            s.Move(-0.5f); s.Tick(0.2f); Require(!s.FlashOn && s.Direction == -1);
        });
        check("Right turn cancels after steering returns to center", () =>
        {
            var s = Latch(1); s.ObserveTurn(20, 25, true);
            Require(s.Latched); s.ObserveTurn(0, 0, true);
            Require(s.Direction == 0 && s.StalkPosition == 0);
        });
        check("Left turn uses negative steering and heading", () =>
        {
            var s = Latch(-1); s.ObserveTurn(-20, -25, true);
            Require(s.Latched); s.ObserveTurn(-2, -1, true); Require(s.Direction == 0);
        });
        check("A parked wheel movement does not cancel the signal", () =>
        {
            var s = Latch(1); s.ObserveTurn(30, 30, false); s.ObserveTurn(0, 0, true);
            Require(s.Latched);
        });
        check("Wheel movement without an actual heading change does not cancel", () =>
        {
            var s = Latch(-1); s.ObserveTurn(-30, 0, true); s.ObserveTurn(0, 0, true);
            Require(s.Latched);
        });
        check("An opposite-direction turn does not cancel the intended signal", () =>
        {
            var s = Latch(1); s.ObserveTurn(-30, -40, true); s.ObserveTurn(0, 0, true);
            Require(s.Latched);
        });
        check("Cancellation waits while the driver manually holds full travel", () =>
        {
            var s = new TurnSignalState(); s.Move(1);
            s.ObserveTurn(30, 30, true); s.ObserveTurn(0, 0, true); Require(s.Latched);
            s.Release(); s.ObserveTurn(0, 0, true); Require(s.Direction == 0);
        });
        check("Short signals do not use steering cancellation", () =>
        {
            var s = new TurnSignalState(); s.Move(-0.5f); s.Release();
            s.ObserveTurn(-30, -30, true); s.ObserveTurn(0, 0, true);
            Require(s.Direction == -1 && !s.Latched);
        });
        check("Invalid signal inputs do not alter the running state", () =>
        {
            var s = Latch(-1); s.Move(float.NaN); s.Tick(float.PositiveInfinity);
            s.ObserveTurn(float.NaN, -30, true); Require(s.Direction == -1 && s.Latched && s.FlashOn);
        });
        check("Short-flash count and timing are configurable", () =>
        {
            var s = new TurnSignalState(1, 0.2f); s.Move(0.5f); s.Release();
            s.Tick(0.2f); Require(s.Direction == 0);
        });
        check("Invalid signal configuration is rejected", () =>
        {
            bool rejected = false;
            try { new TurnSignalState(3, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected);
        });
    }
}
