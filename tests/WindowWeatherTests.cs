using System;
using System.Collections.Generic;
using System.Linq;
using HalfGripSlapPack;

/// <summary>Visibility, exposure, clearing and aperture regressions for unwiped glass.</summary>
internal static class WindowWeatherTests
{
	private static readonly PanePoint[] Opening = { new PanePoint(-0.5f, 0), new PanePoint(0.5f, 0), new PanePoint(0.5f, 0.5f), new PanePoint(-0.5f, 0.5f) };
	private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
	private static void RunFor(WindowWeatherState field, int steps, float rain, float snow, float incidence = 0, float x = 0, float y = 0, float open = 0)
	{
		for (int i = 0; i < steps; i++) field.Step(0.05f, rain, snow, incidence, x, y, open);
	}

	internal static void Run(Action<string, Action> check)
	{
		check("Side/rear dry starts remain clear", () =>
		{
			foreach (bool rear in new[] { false, true })
			{
				var f = new WindowWeatherState(Opening, rear, 1); RunFor(f, 500, 0, 0);
				Require(f.Water.Sum() == 0 && f.Snow.Sum() == 0, "Dry glass received deposits");
			}
		});
		check("Ten-minute mixed downpour preserves strict side/rear opacity ceilings and sparse coverage", () =>
		{
			foreach (bool rear in new[] { false, true })
			{
				var f = new WindowWeatherState(Opening, rear, 2);
				for (int i = 0; i < 12000; i++)
				{
					f.Step(0.05f, 1, 1, 100, 0, 0, 0);
					for (int k = 0; k < f.Water.Length; k++)
						Require(f.Opacity(k) <= (rear ? 0.24f : 0.32f), "Unbounded storm opacity");
				}
				float average = Enumerable.Range(0, f.Water.Length).Average(k => f.Opacity(k));
				Require(average < 0.06f, "Full window film formed: " + average);
			}
		});
		check("Rain and snow clear naturally without washers or blades", () =>
		{
			var f = new WindowWeatherState(Opening, true, 3); RunFor(f, 1200, 1, 1);
			float wet = f.Water.Sum(), snow = f.Snow.Sum();
			Require(wet > 0 && snow > 0, "Storm did not deposit both types");
			RunFor(f, 2400, 0, 0);
			Require(f.Water.Sum() < wet * 0.001f && f.Snow.Sum() < snow * 0.001f, "Deposits became permanent");
		});
		check("Rear receives less forward-weather deposition than side glass", () =>
		{
			var side = new WindowWeatherState(Opening, false, 4); var rear = new WindowWeatherState(Opening, true, 4);
			RunFor(side, 1200, 1, 0); RunFor(rear, 1200, 1, 0);
			Require(side.Water.Sum() > rear.Water.Sum(), "Rear exposure did not differ from side");
		});
		check("Normal incidence increases exposure on windward glass", () =>
		{
			var sheltered = new WindowWeatherState(Opening, false, 5); var windward = new WindowWeatherState(Opening, false, 5);
			RunFor(sheltered, 1200, 1, 0); RunFor(windward, 1200, 1, 0, 70);
			Require(windward.Water.Sum() > sheltered.Water.Sum() * 1.3f, "Incidence ignored");
		});
		check("Side water follows signed airflow while rear beads move more slowly", () =>
		{
			var right = new WindowWeatherState(Opening, false, 6); var left = new WindowWeatherState(Opening, false, 6);
			var rear = new WindowWeatherState(Opening, true, 6);
			int center = 16 * WindowWeatherState.Width + 32;
			right.Water[center] = left.Water[center] = rear.Water[center] = 0.6f;
			RunFor(right, 20, 0, 0, 0, 0.05f); RunFor(left, 20, 0, 0, 0, -0.05f); RunFor(rear, 20, 0, 0, 0, 0.05f);
			float Centroid(WindowWeatherState f) => Enumerable.Range(0, f.Water.Length).Sum(k => (k % WindowWeatherState.Width) * f.Water[k]) / f.Water.Sum();
			Require(Centroid(left) < 32 && Centroid(right) > 32, "Airflow direction lost");
			Require(Centroid(rear) < Centroid(right), "Rear bead runoff matches side streaks");
		});
		check("Snow favors edges while the center remains lightly dusted", () =>
		{
			var f = new WindowWeatherState(Opening, false, 7); RunFor(f, 2400, 0, 1);
			var rim = new List<float>(); var middle = new List<float>();
			for (int y = 0; y < WindowWeatherState.Height; y++)
				for (int x = 0; x < WindowWeatherState.Width; x++)
				{
					float value = f.Snow[y * WindowWeatherState.Width + x];
					if (x < 3 || x >= 61 || y < 3 || y >= 29) rim.Add(value);
					else if (x > 16 && x < 48 && y > 8 && y < 24) middle.Add(value);
				}
			Require(rim.Average() > middle.Average() * 3, "Snow did not favor the edges");
		});
		check("Fully lowered windows receive no rain or snow", () =>
		{
			var f = new WindowWeatherState(Opening, false, 8); RunFor(f, 2400, 1, 1, 100, 0, 0, 1);
			Require(f.Water.Sum() == 0 && f.Snow.Sum() == 0, "Open aperture received precipitation");
		});
		check("Partially lowered panes receive precipitation only on glass above the sill", () =>
		{
			var f = new WindowWeatherState(Opening, false, 9); RunFor(f, 1200, 1, 1, 0, 0, 0, 0.5f);
			Require(f.Water.Take(16 * WindowWeatherState.Width).Sum() == 0, "Hidden glass received impacts");
			Require(f.Water.Sum() > 0, "Remaining exposed glass stayed dry");
		});
		check("Invalid weather/flow and pause ticks cannot corrupt the field", () =>
		{
			var f = new WindowWeatherState(Opening, false, 10);
			f.Step(0, 1, 1, 0, 0, 0, 0); f.Step(float.NaN, 1, 1, 0, 0, 0, 0);
			RunFor(f, 20, float.NaN, float.PositiveInfinity, float.NaN, float.NaN, float.NegativeInfinity);
			Require(f.Water.All(v => v == 0) && f.Snow.All(v => v == 0), "Invalid inputs corrupted field");
		});
		check("Vanilla rain creates visible connected side streaks while preserving clear glass", () =>
		{
			var f = new WindowWeatherState(Opening, false, 71);
			RunFor(f, 400, 0.5f, 0, 0, 0.08f, -0.10f);
			int longest = LongestStreak(f);
			float visible = Enumerable.Range(0, f.Water.Length).Count(k => f.Opacity(k) > 0.08f) / (float)f.Water.Length;
			float mean = Enumerable.Range(0, f.Water.Length).Average(k => f.Opacity(k));
			Console.WriteLine("Vanilla side rain: longest trail=" + longest + " texels, visible fraction=" + visible + ", mean opacity=" + mean);
			Require(longest >= 6, "Rain remained isolated dots instead of connected streaks");
			Require(visible > 0.025f && visible < 0.25f, "Wet features are too faint or cover too much glass");
			Require(mean < 0.06f, "Streaks formed a blanket film");
		});
		check("Ten-minute moving downpour keeps streak opacity and coverage bounded", () =>
		{
			foreach (bool rear in new[] { false, true })
			{
				var f = new WindowWeatherState(Opening, rear, 72);
				for (int i = 0; i < 12000; i++)
				{
					f.Step(0.05f, 1, 0, 100, 0.15f, -0.10f, 0);
					if (i % 200 != 0) continue;
					float mean = Enumerable.Range(0, f.Water.Length).Average(k => f.Opacity(k));
					Require(mean < 0.06f, "Prolonged moving rain obscures the window: rear=" + rear + ", mean=" + mean);
					Require(Enumerable.Range(0, f.Water.Length).All(k => f.Opacity(k) <= (rear ? 0.24f : 0.32f)), "Streak exceeded local opacity cap");
				}
			}
		});
		check("Existing rain trails respond to reversing tangential airflow", () =>
		{
			var right = new WindowWeatherState(Opening, false, 76);
			var left = new WindowWeatherState(Opening, false, 76);
			RunFor(right, 200, 0.5f, 0); RunFor(left, 200, 0.5f, 0);
			RunFor(right, 20, 0, 0, 0, 0.14f); RunFor(left, 20, 0, 0, 0, -0.14f);
			float Centroid(WindowWeatherState f) => Enumerable.Range(0, f.Trail.Length).Sum(k => k % WindowWeatherState.Width * f.Trail[k]) / f.Trail.Sum();
			Require(Centroid(right) > Centroid(left) + 1, "Runoff trails ignored reversed airflow");
		});

		check("Rivulets and their trails drain away after rain stops", () =>
		{
			var f = new WindowWeatherState(Opening, false, 73);
			RunFor(f, 400, 0.5f, 0, 0, 0.08f, -0.10f);
			Require(f.Trail.Sum() > 0, "No runoff to check");
			RunFor(f, 2400, 0, 0, 0, 0.08f, -0.10f);
			Require(f.Trail.Sum() < 0.001f && f.Water.Sum() < 0.001f, "Runoff leaves permanent marks");
		});
		check("Fully lowered glass and snow-only weather cannot create rain trails", () =>
		{
			var open = new WindowWeatherState(Opening, false, 74);
			RunFor(open, 400, 1, 0, 0, 0.08f, -0.10f, 1);
			Require(open.Trail.Sum() == 0, "Runoff appeared in an open aperture");
			var snow = new WindowWeatherState(Opening, false, 74);
			RunFor(snow, 400, 0, 1, 0, 0.08f, -0.10f);
			Require(snow.Trail.Sum() == 0 && snow.Snow.Sum() > 0, "Snow generated liquid streaks");
		});
		check("Rear glass retains shorter runoff paths than side glass", () =>
		{
			var side = new WindowWeatherState(Opening, false, 75);
			var rear = new WindowWeatherState(Opening, true, 75);
			RunFor(side, 400, 0.5f, 0, 0, 0, -0.10f);
			RunFor(rear, 400, 0.5f, 0, 0, 0, -0.025f);
			Require(LongestStreak(side) > LongestStreak(rear), "Rear behaves like a side window");
		});

		check("Moving weather triangle clips at sloping aperture with preserved glass coordinates", () =>
		{
			var opening = new[] { new PanePoint(0, 0), new PanePoint(2, 0), new PanePoint(1.5f, 1), new PanePoint(0, 1) };
			var triangle = new List<PanePoint> { new PanePoint(0, -0.5f), new PanePoint(2, -0.5f), new PanePoint(1.5f, 0.5f) };
			List<PanePoint> clipped = WindowPolygon.Clip(triangle, opening);
			Require(clipped.Count >= 3 && clipped.All(p => p.Y >= 0 && p.Y <= 0.5f), "Triangle escaped the aperture");
			var f = new WindowWeatherState(opening, false, 11);
			Require(clipped.All(f.Contains), "Clipping crossed a sloping edge");
		});
	}

	/// <summary>Measures the span of connected visible runoff pixels, excluding isolated droplets.</summary>
	private static int LongestStreak(WindowWeatherState field)
	{
		var visited = new bool[field.Trail.Length];
		var queue = new Queue<int>(); int longest = 0;
		for (int k = 0; k < visited.Length; k++)
		{
			if (visited[k] || field.Trail[k] < 0.15f) continue;
			int minX = k % WindowWeatherState.Width, maxX = minX;
			int minY = k / WindowWeatherState.Width, maxY = minY;
			visited[k] = true; queue.Enqueue(k);
			while (queue.Count > 0)
			{
				int current = queue.Dequeue(), x = current % WindowWeatherState.Width, y = current / WindowWeatherState.Width;
				minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
				for (int oy = -1; oy <= 1; oy++)
					for (int ox = -1; ox <= 1; ox++)
					{
						int nx = x + ox, ny = y + oy;
						if (nx < 0 || nx >= WindowWeatherState.Width || ny < 0 || ny >= WindowWeatherState.Height) continue;
						int next = ny * WindowWeatherState.Width + nx;
						if (visited[next] || field.Trail[next] < 0.15f) continue;
						visited[next] = true; queue.Enqueue(next);
					}
			}
			longest = Math.Max(longest, Math.Max(maxX - minX + 1, maxY - minY + 1));
		}
		return longest;
	}

}
