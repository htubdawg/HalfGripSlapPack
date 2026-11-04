using System;
using HalfGripSlapPack;

internal static class SignalStalkLayoutTests
{
    private static void Equal(float expected, float actual)
    {
        if (Math.Abs(expected - actual) > 0.00001f) throw new Exception("Stalk artwork alignment changed.");
    }

    internal static void Run(Action<string, Action> check)
    {
        check("Stalk artwork grip stays centered on the native grab point", () =>
        {
            foreach (float length in new[] { 2f, 4.37f, 6.5f })
            {
                var layout = new SignalStalkLayout(length);
                Equal(-length, (layout.MapX(SignalStalkLayout.SourceLeft) + layout.MapX(SignalStalkLayout.GripCut)) * 0.5f);
            }
        });
        check("Changing stalk length does not stretch the grip or rubber boot", () =>
        {
            var shortStalk = new SignalStalkLayout(2);
            var longStalk = new SignalStalkLayout(6.5f);
            Equal(SignalStalkLayout.GripWidth, shortStalk.MapX(SignalStalkLayout.GripCut) - shortStalk.MapX(SignalStalkLayout.SourceLeft));
            Equal(SignalStalkLayout.GripWidth, longStalk.MapX(SignalStalkLayout.GripCut) - longStalk.MapX(SignalStalkLayout.SourceLeft));
            Equal(shortStalk.MapX(SignalStalkLayout.BootCut), longStalk.MapX(SignalStalkLayout.BootCut));
        });
        check("Art sections join the grip to the shaft and boot without gaps", () =>
        {
            var layout = new SignalStalkLayout(4.37f);
            Equal(layout.GripRight, layout.MapX(SignalStalkLayout.GripCut));
            Equal(layout.BootLeft, layout.MapX(SignalStalkLayout.BootCut));
            float mid = layout.MapX((SignalStalkLayout.GripCut + SignalStalkLayout.BootCut) * 0.5f);
            Equal((layout.GripRight + layout.BootLeft) * 0.5f, mid);
        });
        check("Mounting cap and artwork center align with the lever pivot plane", () =>
        {
            Equal(0, new SignalStalkLayout(4.37f).MapX(SignalStalkLayout.SourceRight));
            Equal(0, SignalStalkLayout.MapY(SignalStalkLayout.SourceCenterY));
        });
        check("Invalid artwork lengths are rejected instead of overlapping sections", () =>
        {
            foreach (float length in new[] { 0, 0.5f, float.NaN, float.PositiveInfinity })
            {
                bool rejected = false;
                try { new SignalStalkLayout(length); } catch (ArgumentOutOfRangeException) { rejected = true; }
                if (!rejected) throw new Exception("Invalid stalk artwork layout accepted.");
            }
        });
    }
}
