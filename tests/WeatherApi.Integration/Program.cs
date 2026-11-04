using System;
using HalfGripSlapPack;
using HalfGripOpenWorld;
using UnityEngine;

/// <summary>Runs the actual upstream public weather source through SlapPack's delegate binder, without a game runtime.</summary>
internal static class Program
{
	private static void Assert(bool pass, string message) { if (!pass) throw new Exception(message); }
	private static void Main()
	{
		Assert(OptionalWeatherApi<Vector3>.TryBind(typeof(WeatherView), out var api, out string reason), reason);
		Origin.Active = true; WeatherView.Clear();
		Assert(!api.TryRead(out float rain, out float snow, out Vector3 wind) && rain == 0 && snow == 0 && wind == Vector3.zero, "API should be unavailable before Set.");
		WeatherView.Set(0.2f, 0.7f, new Vector3(-3, 0, 4), 0.9f, 0.6f, 11);
		Assert(api.TryRead(out rain, out snow, out wind) && rain == 0.2f && snow == 0.7f && wind.x == -3 && wind.y == 0 && wind.z == 4, "Sleet or world wind was changed by binding.");
		Origin.Active = false;
		Assert(!api.TryRead(out rain, out snow, out wind) && rain == 0 && snow == 0 && wind == Vector3.zero, "Scene transition leaked weather.");
		Origin.Active = true; WeatherView.Clear();
		Assert(!api.TryRead(out rain, out snow, out wind), "Clear did not invalidate the snapshot.");
		WeatherView.Set(1, 0, new Vector3(5, 0, 0), 1, 1, 12);
		for (int i = 0; i < 1000; i++) api.TryRead(out _, out _, out _);
		long before = GC.GetAllocatedBytesForCurrentThread();
		for (int i = 0; i < 10000; i++) api.TryRead(out _, out _, out _);
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert(allocated == 0, "Public weather polling allocated " + allocated + " bytes.");
		Console.WriteLine("PASS real upstream WeatherView v1: initial/inactive, sleet/wind, origin transition, clear/restart and 10,000 allocation-free delegate polls.");
	}
}
