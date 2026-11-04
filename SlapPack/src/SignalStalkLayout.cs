using System;

namespace HalfGripSlapPack
{
    // Three source-image sections preserve the grip and bellows while only the metal shaft
    // stretches to the configured length. Source coordinates use Unity's bottom-left origin.
    internal sealed class SignalStalkLayout
    {
        internal const int TextureWidth = 2172, TextureHeight = 724;
        internal const float SourceLeft = 20, GripCut = 750, BootCut = 1800, SourceRight = 2151;
        internal const float SourceBottom = 163, SourceTop = 538, SourceCenterY = 345;
        internal const float GripWidth = 0.9f;
        internal const float PixelScale = GripWidth / (GripCut - SourceLeft);
        internal readonly float GripLeft, GripRight, BootLeft;

        internal SignalStalkLayout(float length)
        {
            BootLeft = -(SourceRight - BootCut) * PixelScale;
            if (float.IsNaN(length) || float.IsInfinity(length) || length <= GripWidth * 0.5f - BootLeft)
                throw new ArgumentOutOfRangeException(nameof(length));
            GripLeft = -length - GripWidth * 0.5f;
            GripRight = -length + GripWidth * 0.5f;
        }

        internal float MapX(float sourceX)
        {
            if (sourceX <= GripCut) return GripLeft + (sourceX - SourceLeft) * PixelScale;
            if (sourceX >= BootCut) return (sourceX - SourceRight) * PixelScale;
            return GripRight + (sourceX - GripCut) / (BootCut - GripCut) * (BootLeft - GripRight);
        }

        internal static float MapY(float sourceY) => (sourceY - SourceCenterY) * PixelScale;
    }
}
