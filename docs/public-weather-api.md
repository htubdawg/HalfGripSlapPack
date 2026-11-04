# Open World WeatherView v1 integration

The public publisher is available on [claude/nifty-babbage-6o4kay](https://github.com/htubdawg/HalfGripOpenWorld/tree/claude/nifty-babbage-6o4kay), since Open World 0.24.0. SlapPack 0.8.1 consumes this contract without a compile-time reference to Open World. No upstream branch changes or merges are made here.

`HalfGripOpenWorld.WeatherView.ApiVersion` is a public static `int` getter returning 1. SlapPack validates that version and binds this public method once:

```csharp
public static bool TryGetLocalWeather(out float rain, out float snow, out UnityEngine.Vector3 wind)
```

Rain and snow are the final eased local falling intensities, after cutoff and height splitting but before particle graphics budgets. Their sum is at most 1. Wind is world-space Unity units/second, in the direction the air moves (x east, z north, y zero). False means inactive, with zero output values. The publisher owns activation, snow share, cutoff and floating-origin handling.

`OptionalWeatherApi<TWind>` validates public version/signature and creates a strongly typed delegate. Polling does not use `MethodInfo.Invoke`, a reflected snapshot struct, argument arrays or boxed wind values. Discovery retries for load order. Vanilla runs independently when Open World is absent/inactive; an installed incompatible publisher warns once and falls back to fixed vanilla flags. Private Sky/Storms/Network field reads are removed.

## Reproduce the contract check

Check out the API branch separately and point the harness at its source and the private game references:

```bash
source /workspace/setup-halfgrip/activate.sh
dotnet run --project tests/WeatherApi.Integration/WeatherApi.Integration.csproj -c Release \
  -p:OpenWorldSrcDir=/path/to/HalfGripOpenWorld/OpenWorld/src \
  -p:HalfGripRefsDir=/workspace/HalfGripRefs
```

The harness links the actual upstream WeatherView.cs; it does not vendor or reimplement the publisher. Only its internal `Origin.Active` dependency is stubbed. It checks inactive values, sleet, unchanged world wind, origin activation, Clear/restart and 10,000 delegate polls with zero managed allocation after warmup. The ordinary model tests separately reject unsupported versions and signatures without Unity references.

Live optional-mod loading, scene changes and rendering still need an in-game check. With verbose logging, the active provider should read `Open World WeatherView v1`. Test the two mod branches together; the older 0.23.9 publisher in the supplied 0.8.0 log does not have this public contract.
