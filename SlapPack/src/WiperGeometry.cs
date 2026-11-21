using System;
using System.Collections.Generic;
using System.Linq;

namespace HalfGripSlapPack
{
	/// <summary>
	/// Tandem wiper layout: both blades park pointing toward the passenger side and sweep up together.
	/// The driver blade is long enough to reach past the passenger pivot, so the two swept areas
	/// overlap in the middle of the glass instead of forming two separate fans.
	/// </summary>
	internal sealed class WiperGeometry
	{
		internal readonly PanePoint Pivot;
		internal readonly float Radius;
		internal const float ParkDegrees = 5, TravelDegrees = 90;
		internal const float DriverPivot = 0.18f, PassengerPivot = 0.48f;

		internal WiperGeometry(IReadOnlyList<PanePoint> opening, bool passenger)
		{
			if (opening.Count < 3) throw new ArgumentException("Windshield opening is empty.");
			float minX = opening.Min(p => p.X), maxX = opening.Max(p => p.X);
			float minY = opening.Min(p => p.Y), maxY = opening.Max(p => p.Y);
			float width = maxX - minX, height = maxY - minY;
			if (width < 0.5f || height < 0.3f) throw new ArgumentException("Windshield opening is too small.");
			float x = minX + width * (passenger ? PassengerPivot : DriverPivot);
			float bottom = float.PositiveInfinity;
			for (int i = 0; i < opening.Count; i++)
			{
				PanePoint a = opening[i], b = opening[(i + 1) % opening.Count];
				if (x < Math.Min(a.X, b.X) || x > Math.Max(a.X, b.X) || Math.Abs(b.X - a.X) < 0.00001f) continue;
				bottom = Math.Min(bottom, a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X));
			}
			Pivot = new PanePoint(x, bottom + height * 0.035f);
			float radius = Math.Min(width * 0.55f, height * 0.9f);
			// Constrain the whole sweep, not just the parked blade. The margin also clears
			// the rubber blade's width and the pillars of trapezoidal windshields.
			for (int degree = 0; degree <= (int)TravelDegrees; degree++)
			{
				double angle = (ParkDegrees + degree) * Math.PI / 180;
				float dx = (float)Math.Cos(angle), dy = (float)Math.Sin(angle);
				for (int i = 0; i < opening.Count; i++)
				{
					PanePoint a = opening[i], b = opening[(i + 1) % opening.Count];
					float ex = b.X - a.X, ey = b.Y - a.Y;
					float atPivot = ex * (Pivot.Y - a.Y) - ey * (Pivot.X - a.X);
					float alongRay = ex * dy - ey * dx;
					if (alongRay < -0.00001f) radius = Math.Min(radius, -atPivot / alongRay * 0.94f);
				}
			}
			if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < width * 0.12f)
				throw new ArgumentException("Cannot fit a safe wiper sweep in this opening.");
			Radius = radius;
		}

		internal PanePoint Point(float radial, float sideways, float sweep)
		{
			SweepAxes(sweep, out float c, out float s);
			return Point(radial, sideways, c, s);
		}

		/// <summary>Calculates the shared rotation once, rather than for every rubber/arm corner.</summary>
		internal static void SweepAxes(float sweep, out float c, out float s)
		{
			double angle = (ParkDegrees + TravelDegrees * Math.Max(0, Math.Min(1, sweep))) * Math.PI / 180;
			c = (float)Math.Cos(angle); s = (float)Math.Sin(angle);
		}

		/// <summary>Uses the same float operations with already calculated axes.</summary>
		internal PanePoint Point(float radial, float sideways, float c, float s)
		{
			return new PanePoint(Pivot.X + radial * c - sideways * s, Pivot.Y + radial * s + sideways * c);
		}
	}
}
