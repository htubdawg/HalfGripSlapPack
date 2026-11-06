using System;
using System.Numerics;
using HalfGripSlapPack;

internal static class HeldGunDepthTests
{
	private static void Equal(float a, float b)
	{
		if (Math.Abs(a - b) > 0.000002f) throw new Exception($"Expected {a}, got {b}.");
	}

	internal static void Run(Action<string, Action> check)
	{
		check("3D gun depth compression keeps perspective silhouette and frustum edges unchanged", () =>
		{
			// Off-axis perspective, including the range of near/far planes used by cockpit cameras.
			foreach (float near in new[] { 0.01f, 0.1f, 0.5f })
			{
				const float far = 1500;
				Vector4 zRow = new Vector4(0, 0, -(far + near) / (far - near), -2 * far * near / (far - near));
				Vector4 wRow = new Vector4(0, 0, -1, 0);
				Vector4 maskRow = new Vector4(HeldGunDepth.Compress(zRow.X, wRow.X, -1), HeldGunDepth.Compress(zRow.Y, wRow.Y, -1),
					HeldGunDepth.Compress(zRow.Z, wRow.Z, -1), HeldGunDepth.Compress(zRow.W, wRow.W, -1));
				foreach (float depth in new[] { near, near * 2, 2f, 8f, far })
				{
					Vector4 point = new Vector4(0.4f, -0.3f, -depth, 1);
					float w = Vector4.Dot(wRow, point), z = Vector4.Dot(zRow, point);
					float mask = Vector4.Dot(maskRow, point) / w;
					Equal(HeldGunDepth.Compress(z, w, -1) / w, mask);
					if (mask < -1.000002f || mask > -0.99799f) throw new Exception("Gun escaped the foreground depth range.");
					// X/Y/W rows are passed through: view direction and pixel coverage stay identical.
					Vector4 original = new Vector4(point.X * 1.8f + point.Z * 0.2f, point.Y * 2.2f, z, w);
					Vector4 compressed = original; compressed.Z = Vector4.Dot(maskRow, point);
					Equal(original.X / original.W, compressed.X / compressed.W);
					Equal(original.Y / original.W, compressed.Y / compressed.W);
				}
			}
		});
		check("Gun depth compression preserves near clipping rather than flattening hidden geometry into view", () =>
		{
			foreach (float w in new[] { 0.01f, 0.1f, 1f, 10f })
			{
				Equal(-1, HeldGunDepth.Compress(-w, w, -1) / w);
				if (HeldGunDepth.Compress(-2 * w, w, -1) / w >= -1) throw new Exception("Near-clipped geometry became visible.");
			}
		});
		check("3D gun depth remains in front of glass through ordinary and reversed GPU depth conversion", () =>
		{
			foreach (float gun in new[] { -0.8f, 0f, 0.99f })
			{
				float mask = HeldGunDepth.Compress(gun, 1, -1);
				const float glass = -0.5f;
				if (!(mask < glass && (mask + 1) / 2 < (glass + 1) / 2 && (1 - mask) / 2 > (1 - glass) / 2))
					throw new Exception("Foreground depth ordering inverted.");
			}
		});
	}
}
