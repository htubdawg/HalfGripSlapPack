using System;
using HalfGripSlapPack;

/// <summary>Public fixtures exercise version/signature checks without loading Unity or Open World.</summary>
public static class WeatherApiV1Fixture
{
	public static int ApiVersion => 1;
	public static bool Active;
	public static bool TryGetLocalWeather(out float rain, out float snow, out int wind)
	{
		rain = Active ? 0.2f : 0; snow = Active ? 0.7f : 0; wind = Active ? 5 : 0; return Active;
	}
}

public static class WeatherApiV2Fixture { public static int ApiVersion => 2; }
public static class WeatherApiWrongSignatureFixture
{
	public static int ApiVersion => 1;
	public static bool TryGetLocalWeather(out double rain, out double snow, out int wind) { rain = snow = wind = 0; return false; }
}

/// <summary>Verifies the optional public contract binder rather than private publisher implementation fields.</summary>
internal static class OptionalWeatherApiTests
{
	internal static void Run(Action<string, Action> check)
	{
		check("Public v1 weather delegate preserves mixed precipitation, wind and inactive transitions", () =>
		{
			if (!OptionalWeatherApi<int>.TryBind(typeof(WeatherApiV1Fixture), out var api, out string reason)) throw new Exception(reason);
			WeatherApiV1Fixture.Active = true;
			if (!api.TryRead(out float rain, out float snow, out int wind) || rain != 0.2f || snow != 0.7f || wind != 5) throw new Exception("Public snapshot was altered.");
			WeatherApiV1Fixture.Active = false;
			if (api.TryRead(out rain, out snow, out wind) || rain != 0 || snow != 0 || wind != 0) throw new Exception("Inactive API retained old weather.");
		});
		check("Unsupported weather API versions and signatures are rejected without a hard dependency", () =>
		{
			foreach (Type type in new[] { typeof(WeatherApiV2Fixture), typeof(WeatherApiWrongSignatureFixture), null })
				if (OptionalWeatherApi<int>.TryBind(type, out _, out string reason) || string.IsNullOrEmpty(reason)) throw new Exception("Unsupported contract accepted.");
		});
	}
}
