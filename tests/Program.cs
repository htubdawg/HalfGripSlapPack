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
        Check("Hand clears a mirror approaching or receding through its old depth", () =>
        {
            for (float mirrorDepth = 2; mirrorDepth <= 20; mirrorDepth += 0.25f)
            {
                float depth = HandDepth.InFrontOfMirror(8, 8, mirrorDepth, 0.1f);
                if (depth > mirrorDepth - HandDepth.Clearance + 0.0001f)
                    throw new Exception("Hand intersects mirror at depth " + mirrorDepth);
            }
        });
        Check("Hand already in front retains its depth", () =>
        {
            Equal(5, HandDepth.InFrontOfMirror(5, 5, 8, 0.1f));
        });
        Check("The entire hand clears the glass, including depth beyond its pivot", () =>
        {
            float depth = HandDepth.InFrontOfMirror(8, 8.4f, 7, 0.1f);
            Equal(7 - HandDepth.Clearance, depth + 0.4f);
        });
        Check("Clearance respects near clipping and never pushes a hand backwards", () =>
        {
            Equal(0.15f, HandDepth.InFrontOfMirror(8, 8, 0.1f, 0.1f));
            Equal(0.12f, HandDepth.InFrontOfMirror(0.12f, 0.12f, 0.1f, 0.1f));
        });
        Check("Interior inclusion adds the required camera layers", () =>
        {
            var mask = new MirrorCullingMask(1, 1 << 8);
            Equal(1 | (1 << 8), mask.Apply(1, true));
        });
        Check("Disabling restores the original mask after enabling", () =>
        {
            var mask = new MirrorCullingMask(1, (1 << 8) | (1 << 9));
            Equal(1, mask.Apply(mask.Apply(1, true), false));
        });
        Check("Interior layers present before the mod are preserved on restore", () =>
        {
            int original = 1 | (1 << 8);
            var mask = new MirrorCullingMask(original, (1 << 8) | (1 << 9));
            Equal(original, mask.Apply(mask.Apply(original, true), false));
        });
        Check("Unrelated camera layer changes survive toggling", () =>
        {
            var mask = new MirrorCullingMask(1, 1 << 8);
            int changed = (mask.Apply(1, true) & ~1) | (1 << 10);
            Equal(1 << 10, mask.Apply(changed, false));
        });
        Check("Window starts raised and lowers proportionally to crank turns", () =>
        {
            var window = new WindowTravel();
            Equal(0, window.OpenFraction);
            window.Turn(360, 3);
            Equal(1f / 3, window.OpenFraction);
            window.Turn(360, 3);
            window.Turn(360, 3);
            Equal(1, window.OpenFraction);
        });
        Check("Reversing the crank raises a partially lowered window", () =>
        {
            var window = new WindowTravel();
            window.Turn(540, 3);
            window.Turn(-270, 3);
            Equal(0.25f, window.OpenFraction);
        });
        Check("Window stops have no accumulated windup", () =>
        {
            var window = new WindowTravel();
            window.Turn(10000, 3);
            window.Turn(-108, 3);
            Equal(0.9f, window.OpenFraction);
            window.Turn(-10000, 3);
            window.Turn(108, 3);
            Equal(0.1f, window.OpenFraction);
        });
        Check("Driver and passenger window travel is independent", () =>
        {
            var driver = new WindowTravel();
            var passenger = new WindowTravel();
            driver.Turn(540, 3);
            Equal(0.5f, driver.OpenFraction);
            Equal(0, passenger.OpenFraction);
        });
        Check("Crank movement unwraps the angular seam in both directions", () =>
        {
            Equal(2, WindowTravel.AngularDelta(179, -179));
            Equal(-2, WindowTravel.AngularDelta(-179, 179));
        });
        Check("Repeated wrapped revolutions produce continuous travel", () =>
        {
            var window = new WindowTravel();
            float previous = 0;
            for (int i = 1; i <= 24; i++)
            {
                float angle = (i * 45 + 180) % 360 - 180;
                window.Turn(WindowTravel.AngularDelta(previous, angle), 3);
                previous = angle;
            }
            Equal(1, window.OpenFraction);
        });
        Check("Invalid crank parameters cannot corrupt travel", () =>
        {
            var window = new WindowTravel();
            window.Turn(float.NaN, 3);
            window.Turn(360, 0);
            window.Turn(360, float.PositiveInfinity);
            Equal(0, window.OpenFraction);
        });
        Check("Half-precision mesh coordinates decode signed normal values", () =>
        {
            Equal(1, MeshBufferData.HalfToSingle(0x3c00));
            Equal(-2, MeshBufferData.HalfToSingle(0xc000));
            Equal(0.5f, MeshBufferData.HalfToSingle(0x3800));
            Equal(65504, MeshBufferData.HalfToSingle(0x7bff));
        });
        Check("Half-precision mesh coordinates preserve zero and subnormals", () =>
        {
            Equal(0, MeshBufferData.HalfToSingle(0));
            Equal(0, MeshBufferData.HalfToSingle(0x8000));
            if (MeshBufferData.HalfToSingle(1) != 1f / 16777216f)
                throw new Exception("Small mesh coordinates were lost.");
        });
        Check("Invalid half-precision coordinates remain identifiable", () =>
        {
            if (!float.IsPositiveInfinity(MeshBufferData.HalfToSingle(0x7c00)) ||
                !float.IsNegativeInfinity(MeshBufferData.HalfToSingle(0xfc00)) ||
                !float.IsNaN(MeshBufferData.HalfToSingle(0x7c01)))
                throw new Exception("Invalid GPU coordinate was masked.");
        });
        Check("Raised window fills its opening and fully lowered glass disappears", () =>
        {
            var opening = WindowPolygon.Hull(new[] { new PanePoint(0, 0), new PanePoint(2, 0), new PanePoint(2, 1), new PanePoint(0, 1) });
            Equal(2, Area(WindowPolygon.Lower(opening, 0)));
            Equal(0, Area(WindowPolygon.Lower(opening, 1)));
        });
        Check("Half-lowered rectangular glass occupies the lower half only", () =>
        {
            var opening = WindowPolygon.Hull(new[] { new PanePoint(0, 0), new PanePoint(2, 0), new PanePoint(2, 1), new PanePoint(0, 1) });
            var lowered = WindowPolygon.Lower(opening, 0.5f);
            Equal(1, Area(lowered));
            foreach (var p in lowered) if (p.Y < -0.0001f || p.Y > 0.5001f) throw new Exception("Glass escaped its door opening.");
        });
        Check("Sloped door glass remains clipped inside its frame throughout travel", () =>
        {
            var opening = WindowPolygon.Hull(new[] { new PanePoint(0, 0), new PanePoint(3, 0), new PanePoint(2, 1), new PanePoint(0, 1) });
            float previous = Area(opening);
            for (int i = 0; i <= 10; i++)
            {
                var polygon = WindowPolygon.Lower(opening, i / 10f);
                float area = Area(polygon);
                if (area > previous + 0.0001f) throw new Exception("Glass area increased while lowering.");
                foreach (var p in polygon)
                    if (p.X < -0.0001f || p.Y < -0.0001f || p.Y > 1.0001f || p.X + p.Y > 3.0001f)
                        throw new Exception("Glass escaped its sloped frame.");
                previous = area;
            }
        });
        Check("Pane hull removes shared triangle vertices without losing the opening", () =>
        {
            var hull = WindowPolygon.Hull(new[] { new PanePoint(0, 0), new PanePoint(2, 0), new PanePoint(2, 1),
                new PanePoint(0, 0), new PanePoint(2, 1), new PanePoint(0, 1), new PanePoint(1, 0.5f) });
            if (hull.Count != 4) throw new Exception("Triangle seams survived in the pane outline.");
            Equal(2, Area(hull));
        });
        TurnSignalTests.Run(Check);
        SignalRelayTests.Run(Check);
        SignalStalkLayoutTests.Run(Check);
        WiperTests.Run(Check);
        Console.WriteLine(passed + " tests passed.");
    }

    private static float Area(System.Collections.Generic.IReadOnlyList<PanePoint> polygon)
    {
        float twice = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
            twice += a.X * b.Y - a.Y * b.X;
        }
        return Math.Abs(twice) / 2;
    }
}
