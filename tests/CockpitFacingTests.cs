using System;
using HalfGripSlapPack;

internal static class CockpitFacingTests
{
	internal static void Run(Action<string, Action> check)
	{
		check("Generated cockpit fans face the cockpit camera (−Z) like the game's sprites", () =>
		{
			// Counter-clockwise outline, as the indicator bezel and lens are listed: the old fan faced +Z.
			float[] xs = { -1, 1, 1, -1 }, ys = { -1, -1, 1, 1 };
			AllFaceCockpit(CockpitFacing.Fan(xs, ys), xs, ys);
			// Clockwise outline: already facing −Z, still oriented the same way.
			float[] cx = { -1, -1, 1, 1 }, cy = { -1, 1, 1, -1 };
			AllFaceCockpit(CockpitFacing.Fan(cx, cy), cx, cy);
		});
		check("Signal and mirrored wiper stalk quads end up facing the same way", () =>
		{
			float[] xs = { 0, -2, -2, 0 }, ys = { -1, -1, 1, 1 };
			float[] mirrored = { 0, 2, 2, 0 };
			int[] left = { 0, 1, 2, 0, 2, 3 }, right = { 0, 1, 2, 0, 2, 3 };
			CockpitFacing.Orient(left, xs, ys);
			CockpitFacing.Orient(right, mirrored, ys);
			AllFaceCockpit(left, xs, ys);
			AllFaceCockpit(right, mirrored, ys);
		});
	}

	private static void AllFaceCockpit(int[] triangles, float[] xs, float[] ys)
	{
		if (triangles.Length == 0) throw new Exception("No triangles were generated.");
		for (int t = 0; t < triangles.Length; t += 3)
		{
			int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
			if (CockpitFacing.NormalZ(xs[a], ys[a], xs[b], ys[b], xs[c], ys[c]) >= 0)
				throw new Exception("A cockpit triangle faces away from the cockpit camera.");
		}
	}
}
