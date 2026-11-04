using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
	/// <summary>Same-frame color-copy settings; there is no capture camera or timer.</summary>
	internal sealed class WindshieldRefractionSettings
	{
		internal readonly ConfigEntry<bool> Enabled;
		internal readonly ConfigEntry<int> Width;
		internal readonly ConfigEntry<float> Strength;

		/// <summary>Preserves the existing width setting and retires rate and manual orientation settings.</summary>
		internal WindshieldRefractionSettings(ConfigFile config)
		{
			Enabled = config.Bind("Wipers", "RefractionEnabled", true, "Same-frame distortion using the cockpit world camera's color. False retains the tint overlay. Restart after changing.");
			Width = config.Bind("Wipers", "RefractionCaptureWidth", 320, new ConfigDescription("Width of the point-filtered scene-color copy, refreshed each visible wet frame without another camera render. Restart after changing.", new AcceptableValueRange<int>(320, 1280)));
			Strength = config.Bind("Wipers", "RefractionStrength", 0.008f, new ConfigDescription("Field-driven sampling offset as a fraction of the view. Restart after changing.", new AcceptableValueRange<float>(0.001f, 0.02f)));
		}
	}

	/// <summary>Projects field-resolution water into the current camera image and owns its render-graph pass.</summary>
	public sealed class WindshieldRefraction : MonoBehaviour
	{
		private CarControllerNew car;
		private WiperWindshield glass;
		private WindshieldWeatherState field;
		private WindshieldRefractionSettings settings;
		private Material material;
		private Mesh mesh;
		private Vector3[] positions;
		private PanePoint[] samples;
		private Vector2[] uv, offsets;
		private Color32[] colors;
		private int[] wetIndices;
		private int wetCount;
		private uint revision = uint.MaxValue;
		private WindshieldRefractionPass pass;
		private Action<string> warning;
		private bool initialized, subscribed, failed, ready;
		private int wantedFrame = -1, drawnFrame = -1;

		internal bool Ready => ready && !failed && car && car.fpvCamera && car.fpvCamera.isActiveAndEnabled;
		internal bool CanDraw => !failed && isActiveAndEnabled;
		internal Camera TargetCamera => car ? car.fpvCamera : null;
		internal Mesh Surface => mesh;
		internal Material SurfaceMaterial => material;
		internal Matrix4x4 ModelMatrix => car.transform.localToWorldMatrix;
		internal int CopyWidth => settings.Width.Value;

		/// <summary>Requires a loaded transparent stock material, preserving its exact compiled keyword set.</summary>
		private static Material StockMaterial(CarControllerNew car)
		{
			foreach (WeatherController weather in Object.FindObjectsByType<WeatherController>(FindObjectsSortMode.None))
			{
				if (weather.gameObject.scene != car.gameObject.scene || !weather.rainEffect) continue;
				foreach (ParticleSystemRenderer renderer in weather.rainEffect.GetComponentsInChildren<ParticleSystemRenderer>(true))
				{
					Material source = renderer.sharedMaterial;
					if (!source || !source.shader || !source.shader.isSupported || source.shader.name != "Universal Render Pipeline/Particles/Unlit") continue;
					if (!source.HasProperty("_BaseMap") || !source.HasProperty("_Surface") || source.GetFloat("_Surface") < 0.5f) continue;
					if (source.HasProperty("_ColorMode") && source.GetFloat("_ColorMode") != 0) continue;
					if (source.HasProperty("_Blend") && source.GetFloat("_Blend") != 0 && source.GetFloat("_Blend") != 1) continue;
					// Depth-fade variants need additional inputs. Retain tint rather than
					// selecting an unverified, potentially stripped keyword combination.
					if (source.IsKeywordEnabled("_SOFTPARTICLES_ON") || source.IsKeywordEnabled("SOFTPARTICLES_ON") || source.IsKeywordEnabled("_FADING_ON") || source.IsKeywordEnabled("_DISTORTION_ON")) continue;
					return source;
				}
			}
			throw new NotSupportedException("No loaded stock transparent multiply-color particle material without depth fading was found.");
		}

		/// <summary>Creates a mesh, material and pass; temporary scene textures belong to the render graph.</summary>
		internal void Initialize(CarControllerNew car, WiperWindshield glass, WindshieldWeatherState field,
			WindshieldRefractionSettings settings, List<Object> owned, Action<string> warning)
		{
			this.car = car; this.glass = glass; this.field = field; this.settings = settings; this.warning = warning;
			Material stock = StockMaterial(car);
			material = new Material(stock) { name = "SlapPack same-frame wet-glass refraction", renderQueue = 3000 }; owned.Add(material);
			material.SetColor("_BaseColor", Color.white);
			// Keep the stock blend factors paired with its straight/premultiplied alpha keywords.
			material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
			mesh = glass.RefractionMesh(field, out samples); owned.Add(mesh); mesh.MarkDynamic();
			positions = mesh.vertices; uv = new Vector2[positions.Length]; offsets = new Vector2[positions.Length];
			colors = new Color32[positions.Length]; wetIndices = new int[positions.Length]; mesh.colors32 = colors;
			pass = new WindshieldRefractionPass(this);
			initialized = true; Subscribe();
		}

		/// <summary>Installs callbacks only for existing cameras, without changing renderer feature lists.</summary>
		private void Subscribe()
		{
			if (!initialized || subscribed) return;
			RenderPipelineManager.beginCameraRendering += BeginCamera;
			RenderPipelineManager.endCameraRendering += EndCamera;
			subscribed = true;
		}

		/// <summary>Queues the pass only for the player's current, visibly wet cockpit view.</summary>
		private void BeginCamera(ScriptableRenderContext context, Camera camera)
		{
			if (failed || !car || camera != car.fpvCamera || !camera.isActiveAndEnabled || !glass.Source || !glass.Source.gameObject.activeInHierarchy) return;
			try
			{
				UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
				if (!data || data.renderType != CameraRenderType.Base || camera.stereoEnabled)
					throw new NotSupportedException("Refraction requires a non-stereo URP base world camera.");
				if (!HasVisibleWater(camera.projectionMatrix * camera.worldToCameraMatrix * car.transform.localToWorldMatrix, camera.nearClipPlane))
				{ ready = false; return; }
				wantedFrame = Time.frameCount;
				data.scriptableRenderer.EnqueuePass(pass);
			}
			catch (Exception error) { Fail(error.Message); }
		}

		/// <summary>Detects unavailable graph execution rather than displaying an older frame.</summary>
		private void EndCamera(ScriptableRenderContext context, Camera camera)
		{
			if (!failed && car && camera == car.fpvCamera && wantedFrame == Time.frameCount && drawnFrame != wantedFrame)
				Fail("The cockpit render-graph pass did not execute; this build requires RenderGraph.");
		}

		/// <summary>Recomputes normals and alpha only when the 20 Hz water/impact fields change.</summary>
		private void UpdateFieldSamples()
		{
			if (revision == field.Revision) return;
			revision = field.Revision; wetCount = 0;
			const int width = WindshieldWeatherState.Width, height = WindshieldWeatherState.Height;
			for (int i = 0; i < positions.Length; i++)
			{
				int x = Mathf.Clamp((int)((samples[i].X - field.MinX) / field.SpanX * width), 0, width - 1);
				int y = Mathf.Clamp((int)((samples[i].Y - field.MinY) / field.SpanY * height), 0, height - 1);
				int k = y * width + x;
				float water = field.Inside[k] ? field.Water[k] : 0;
				colors[i] = new Color32(255, 255, 255, 0); offsets[i] = Vector2.zero;
				if (water <= 0.01f) continue;
				int left = y * width + Math.Max(0, x - 1), right = y * width + Math.Min(width - 1, x + 1);
				int down = Math.Max(0, y - 1) * width + x, up = Math.Min(height - 1, y + 1) * width + x;
				PanePoint offset = WindshieldRefractionMath.Offset(water, field.Water[left] - field.Water[right], field.Water[down] - field.Water[up],
					field.Impact[left] - field.Impact[right], field.Impact[down] - field.Impact[up], settings.Strength.Value);
				offsets[i] = new Vector2(offset.X, offset.Y);
				colors[i].a = (byte)(WindshieldWeatherOptics.Opacity(water, 0) * 255);
				wetIndices[wetCount++] = i;
			}
			mesh.colors32 = colors;
		}

		/// <summary>Skips enqueueing for an off-screen wet area without uploading a projected mesh.</summary>
		private bool HasVisibleWater(Matrix4x4 projection, float near)
		{
			UpdateFieldSamples();
			for (int n = 0; n < wetCount; n++)
			{
				Vector3 p = positions[wetIndices[n]];
				float x = projection.m00 * p.x + projection.m01 * p.y + projection.m02 * p.z + projection.m03;
				float y = projection.m10 * p.x + projection.m11 * p.y + projection.m12 * p.z + projection.m13;
				float w = projection.m30 * p.x + projection.m31 * p.y + projection.m32 * p.z + projection.m33;
				if (WindshieldRefractionMath.Visible(1, x, y, w, near)) return true;
			}
			return false;
		}

		/// <summary>Updates projection every rendered frame and gates the copy on visible wet vertices.</summary>
		internal bool UpdateSurface(Matrix4x4 projection, float near)
		{
			UpdateFieldSamples(); bool visible = false;
			for (int n = 0; n < wetCount; n++)
			{
				int i = wetIndices[n]; Vector3 p = positions[i];
				float x = projection.m00 * p.x + projection.m01 * p.y + projection.m02 * p.z + projection.m03;
				float y = projection.m10 * p.x + projection.m11 * p.y + projection.m12 * p.z + projection.m13;
				float w = projection.m30 * p.x + projection.m31 * p.y + projection.m32 * p.z + projection.m33;
				if (w <= near) continue;
				visible |= WindshieldRefractionMath.Visible(1, x, y, w, near);
				float halfInverseW = 0.5f / w;
				uv[i] = new Vector2(Mathf.Clamp01(x * halfInverseW + 0.5f + offsets[i].x), Mathf.Clamp01(y * halfInverseW + 0.5f + offsets[i].y));
			}
			if (visible) mesh.uv = uv;
			return visible;
		}

		/// <summary>Marks a successful current-frame draw; no captured scene is kept between frames.</summary>
		internal void Drawn() { drawnFrame = Time.frameCount; ready = true; }

		/// <summary>Handles a view changed by another camera hook without treating a culled surface as an error.</summary>
		internal void SkipFrame() { wantedFrame = -1; ready = false; }

		/// <summary>Warns once and leaves the weather overlay as the fallback.</summary>
		internal void Fail(string reason)
		{
			if (failed) return;
			failed = true; ready = false;
			warning("Windshield same-frame refraction disabled; retaining tint: " + reason);
		}

		/// <summary>Reattaches callbacks when a bound surface is enabled again.</summary>
		private void OnEnable() { Subscribe(); }

		/// <summary>Unhooks callbacks before a body rebuild or mod unload.</summary>
		private void OnDisable()
		{
			if (!subscribed) return;
			RenderPipelineManager.beginCameraRendering -= BeginCamera;
			RenderPipelineManager.endCameraRendering -= EndCamera;
			subscribed = false; ready = false;
		}
	}
}
