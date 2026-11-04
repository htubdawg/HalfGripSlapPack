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
						Require(f.Opacity(k) <= (rear ? 0.16f : 0.20f), "Unbounded storm opacity");
				}
				float average = Enumerable.Range(0, f.Water.Length).Average(k => f.Opacity(k));
				Require(average < 0.04f, "Full window film formed: " + average);
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
}
