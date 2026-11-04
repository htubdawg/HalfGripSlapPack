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
        check("Stalk sprite is authored at a small native pixel resolution", () =>
        {
            byte[] pixels = SignalStalkPixels.Build();
            if (SignalStalkLayout.TextureWidth != 128 || SignalStalkLayout.TextureHeight != 20 || pixels.Length != 2560)
                throw new Exception("Stalk sprite resolution increased.");
        });
        check("Stalk pixel art uses a limited opaque palette and transparent background", () =>
        {
            byte[] pixels = SignalStalkPixels.Build();
            if (SignalStalkPixels.Palette.Length > 8 || SignalStalkPixels.Palette[0] != 0)
                throw new Exception("Stalk palette is not limited or transparent.");
            foreach (byte pixel in pixels)
                if (pixel >= SignalStalkPixels.Palette.Length || (pixel != 0 && (SignalStalkPixels.Palette[pixel] & 255) != 255))
                    throw new Exception("Invalid or antialiased stalk pixel.");
        });
        check("Code-drawn grip, shaft and boot form one connected silhouette", () =>
        {
            byte[] pixels = SignalStalkPixels.Build();
            int width = SignalStalkLayout.TextureWidth, height = SignalStalkLayout.TextureHeight;
            for (int x = 0; x < width; x++)
                if (pixels[x] != 0 || pixels[(height - 1) * width + x] != 0) throw new Exception("Sprite reaches texture border.");
            for (int y = 0; y < height; y++)
                if (pixels[y * width] != 0 || pixels[y * width + width - 1] != 0) throw new Exception("Sprite reaches texture border.");
            var pending = new System.Collections.Generic.Queue<int>();
            var reached = new bool[pixels.Length];
            int first = Array.FindIndex(pixels, p => p != 0);
            pending.Enqueue(first); reached[first] = true;
            while (pending.Count > 0)
            {
                int index = pending.Dequeue(), x = index % width, y = index / width;
                foreach (int neighbor in new[] { x > 0 ? index - 1 : -1, x + 1 < width ? index + 1 : -1,
                    y > 0 ? index - width : -1, y + 1 < height ? index + width : -1 })
                    if (neighbor >= 0 && pixels[neighbor] != 0 && !reached[neighbor]) { reached[neighbor] = true; pending.Enqueue(neighbor); }
            }
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i] != 0 && !reached[i]) throw new Exception("Disconnected stalk art section.");
        });
    }
}
