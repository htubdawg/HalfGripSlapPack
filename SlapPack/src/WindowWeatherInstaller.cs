using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HalfGripTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
	/// <summary>Owns side/rear precipitation independently of the optional crank and wiper controls.</summary>
	public sealed class WindowWeatherInstaller : MonoBehaviour
	{
		private ModContext context;
		private ConfigEntry<bool> enabledWeather;
		private WindowInstaller windows;
		private CarControllerNew car;
		private CameraSwitch cameras;
		private WindshieldWeatherSource source;
		private List<WindowWeatherSurface> surfaces = new List<WindowWeatherSurface>();
		private readonly List<Object> owned = new List<Object>();
		private float nextScan, elapsed;
		private int signature;

		internal void Initialize(ModContext ctx, WindowInstaller windows)
		{
			context = ctx; this.windows = windows;
			enabledWeather = ctx.Config.Bind("Windows", "WeatherEffects", true,
				"Sparse rain and light edge snow on all side/rear glass. Drains and clears naturally, with capped opacity; independent of wipers and cranks.");
			ctx.OnSceneLoaded(scene => { RemoveWeather(); nextScan = Time.unscaledTime + 0.5f; });
		}

		private void Update()
		{
			if (context == null) return;
			if (!enabledWeather.Value) { RemoveWeather(); return; }
			if (Time.unscaledTime < nextScan) return;
			nextScan = Time.unscaledTime + 1;
			CarControllerNew target = null;
			foreach (CarControllerNew candidate in Object.FindObjectsByType<CarControllerNew>(FindObjectsSortMode.None))
				if (candidate.isPlayer) { target = candidate; break; }
			CameraSwitch cameraSwitch = target ? target.GetComponent<CameraSwitch>() : null;
			int current = Signature(cameraSwitch);
			if (target == car && cameraSwitch == cameras && current == signature) return;
			RemoveWeather();
			car = target; cameras = cameraSwitch; signature = current;
			if (!car || !cameras) return;
			try
			{
				surfaces = WindowWeatherSurface.Find(car, cameras, owned);
				source = new WindshieldWeatherSource(message => context.Log.LogWarning(message));
				ModLog.Info(context.Log, "Side/rear precipitation attached to " + surfaces.Count + " glass patches; self-clearing, capped opacity.");
			}
			catch (Exception error)
			{
				ClearOwned();
				context.Log.LogWarning("Side/rear precipitation could not fit this body: " + error.Message);
			}
		}

		private static int Signature(CameraSwitch cameras)
		{
			int value = 17;
			if (!cameras) return value;
			foreach (MeshRenderer glass in cameras.windshields ?? new MeshRenderer[0])
			{
				if (!glass || !glass.gameObject.activeInHierarchy) continue;
				MeshFilter filter = glass.GetComponent<MeshFilter>();
				value = unchecked(value * 31 + glass.GetInstanceID());
				if (filter && filter.sharedMesh) value = unchecked(value * 31 + filter.sharedMesh.GetInstanceID());
			}
			return value;
		}

		private void LateUpdate()
		{
			if (!car || source == null || surfaces.Count == 0 || Time.timeScale <= 0) return;
			elapsed += Time.deltaTime;
			if (elapsed < 0.05f) return;
			float dt = Mathf.Min(elapsed, 0.1f); elapsed = 0;
			WindshieldPrecipitation weather = source.Read(car);
			Vector3 velocity = car.rb ? car.rb.linearVelocity : Vector3.zero;
			Vector3 relative = car.transform.InverseTransformDirection(weather.Wind - velocity);
			foreach (WindowWeatherSurface surface in surfaces)
			{
				if (!surface.Source) continue;
				WindowPane moving = windows ? windows.MovingPane(surface.Source, surface.Indices) : null;
				surface.Step(car, weather, relative, dt, moving != null ? moving.OpenFraction : 0);
			}
		}

		private void ClearOwned()
		{
			surfaces.Clear(); source = null; elapsed = 0;
			for (int i = owned.Count - 1; i >= 0; i--)
				if (owned[i]) { if (owned[i] is GameObject go) go.SetActive(false); Object.Destroy(owned[i]); }
			owned.Clear();
		}

		internal void RemoveWeather() { ClearOwned(); car = null; cameras = null; signature = 0; }
		private void OnDestroy() { RemoveWeather(); }
	}
}
