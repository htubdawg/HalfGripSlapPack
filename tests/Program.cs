using System;
using HalfGripSlapPack;

internal static class Program
{
    private static int passed;

    private static void Check(string name, Action test)
    {
        test();
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static void Equal(float expected, float actual)
    {
        if (Math.Abs(expected - actual) > 0.0001f || float.IsNaN(actual))
            throw new Exception("Expected " + expected + ", got " + actual);
    }

    private static void Main()
    {
        Check("Both axes use sensitivity and preserve existing adjustment", () =>
        {
            var a = new MirrorAdjustment(2, -3);
            a.Move(1, 2, 4, 30, 20);
            Equal(6, a.Yaw); Equal(5, a.Pitch);
        });
        Check("Positive stops clamp each axis independently", () =>
        {
            var a = new MirrorAdjustment(0, 0);
            a.Move(100, 100, 8, 30, 20);
            Equal(30, a.Yaw); Equal(20, a.Pitch);
        });
        Check("Negative stops clamp each axis independently", () =>
        {
            var a = new MirrorAdjustment(0, 0);
            a.Move(-100, -100, 8, 30, 20);
            Equal(-30, a.Yaw); Equal(-20, a.Pitch);
        });
        Check("Reversal responds immediately after dragging beyond a stop", () =>
        {
            var a = new MirrorAdjustment(30, 20);
            a.Move(100, 100, 8, 30, 20);
            a.Move(-0.5f, -0.5f, 8, 30, 20);
            Equal(26, a.Yaw); Equal(16, a.Pitch);
        });
        Check("Restored values respect current limits", () =>
        {
            var a = new MirrorAdjustment(55, -40);
            a.Move(0, 0, 1, 15, 10);
            Equal(15, a.Yaw); Equal(-10, a.Pitch);
        });
        Check("Reset clears both axes and subsequent drags start at zero", () =>
        {
            var a = new MirrorAdjustment(25, -10);
            a.Reset();
            Equal(0, a.Yaw); Equal(0, a.Pitch);
            a.Move(1, -1, 2, 30, 20);
            Equal(2, a.Yaw); Equal(-2, a.Pitch);
        });
        Check("Invalid saved values recover to neutral", () =>
        {
            var a = new MirrorAdjustment(float.NaN, float.PositiveInfinity);
            Equal(0, a.Yaw); Equal(0, a.Pitch);
        });
        Check("Invalid movement cannot poison the camera angles", () =>
        {
            var a = new MirrorAdjustment(3, 4);
            a.Move(float.NaN, 1, 8, 30, 20);
            a.Move(1, 1, float.PositiveInfinity, 30, 20);
            Equal(3, a.Yaw); Equal(4, a.Pitch);
        });
        Console.WriteLine(passed + " tests passed.");
    }
}
