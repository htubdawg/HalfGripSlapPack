using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
    internal sealed class WindshieldRefractionSettings
    {
        internal readonly ConfigEntry<bool> Enabled, FlipY;
        internal readonly ConfigEntry<int> Width;
        internal readonly ConfigEntry<float> Hz, Strength;
        internal WindshieldRefractionSettings(ConfigFile config)
        {
            Enabled = config.Bind("Wipers", "RefractionEnabled", true, "Experimental low-resolution camera refraction on wet glass. Restart after changing. False restores the tint overlay.");
            FlipY = config.Bind("Wipers", "RefractionFlipY", false, "Flip the captured image vertically if required by the graphics backend. Restart after changing.");
            Width = config.Bind("Wipers", "RefractionCaptureWidth", 640, new ConfigDescription("Capture width in pixels; height follows cockpit aspect. Restart after changing.", new AcceptableValueRange<int>(320, 1280)));
            Hz = config.Bind("Wipers", "RefractionCaptureHz", 15f, new ConfigDescription("Maximum world captures per second. Lower is cheaper but lags more. Restart after changing.", new AcceptableValueRange<float>(5, 30)));
            Strength = config.Bind("Wipers", "RefractionStrength", 0.008f, new ConfigDescription("Ripple offset as a fraction of the view. Restart after changing.", new AcceptableValueRange<float>(0.001f, 0.02f)));
        }
    }

    // A separate, throttled world render. Vertex colors mask wet areas, while
    // distorted screen UVs sample the image using the game's existing particle shader.
    // No runtime shader compilation, screen readback, or original camera mutation.
    [DefaultExecutionOrder(20000)]
    public sealed class WindshieldRefraction : MonoBehaviour
    {
        private CarControllerNew car;
        private WiperWindshield glass;
        private WindshieldWeatherState field;
        private WindshieldRefractionSettings settings;
        private WindshieldCaptureClock clock;
        private Camera capture;
        private UniversalAdditionalCameraData cameraData;
        private RenderTexture image;
        private Material material;
        private Mesh mesh;
        private MeshRenderer surface;
        private Renderer[] hidden;
        private bool[] previousVisibility;
        private Vector3[] positions;
        private PanePoint[] samples;
        private Vector2[] uv;
        private Color32[] colors;
        private Vector2[] waves;
        private Action<string> warning;
        private bool captured, failed;
        private float wetness;
        private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        private static readonly FieldInfo RendererIndex = AccessTools.Field(typeof(UniversalAdditionalCameraData), "m_RendererIndex");

        internal bool Ready => captured && !failed && car && car.fpvCamera && car.fpvCamera.isActiveAndEnabled && surface && surface.enabled;
        internal void SetWetness(float amount) { wetness = amount; }

        internal void Initialize(CarControllerNew car, WiperWindshield glass, WindshieldWeatherState field, MeshRenderer tint,
            WindshieldRefractionSettings settings, List<Object> owned, Action<string> warning)
        {
            this.car = car; this.glass = glass; this.field = field; this.settings = settings; this.warning = warning;
            clock = new WindshieldCaptureClock(settings.Hz.Value);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) throw new InvalidOperationException("The vertex-color particle shader required for refraction is unavailable.");
            material = new Material(shader) { name = "SlapPack low-resolution wet-glass refraction", renderQueue = 3000 }; owned.Add(material);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_ColorMode", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_AlphaClip", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("SOFTPARTICLES_ON"); material.DisableKeyword("_FADING_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            mesh = glass.RefractionMesh(out samples); owned.Add(mesh); mesh.MarkDynamic();
            positions = mesh.vertices; uv = new Vector2[positions.Length]; colors = new Color32[positions.Length]; waves = new Vector2[positions.Length];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = new Vector2(Mathf.Sin(samples[i].X * 49 + samples[i].Y * 31), Mathf.Cos(samples[i].Y * 57 - samples[i].X * 23));
            mesh.colors32 = colors;
            gameObject.layer = glass.Source.gameObject.layer;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface = gameObject.AddComponent<MeshRenderer>(); surface.sharedMaterial = material; surface.enabled = false;
            surface.shadowCastingMode = ShadowCastingMode.Off; surface.receiveShadows = false;
            var cameraObject = new GameObject("SlapPack.WindshieldCapture"); owned.Add(cameraObject);
            cameraObject.transform.SetParent(car.transform, false);
            capture = cameraObject.AddComponent<Camera>(); capture.enabled = false;
            cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            hidden = car.GetComponentsInChildren<Renderer>(true).Where(r => r == tint || r == surface
                || r.name.StartsWith("SlapPack.WiperBlade.", StringComparison.Ordinal) || r.name == "SlapPack.WasherJets"
                || r.GetComponent<SpeedLines>()).ToArray();
            previousVisibility = new bool[hidden.Length];
        }

        private void LateUpdate()
        {
            if (failed || !capture || !car || !surface) return;
            Camera source = car.fpvCamera;
            bool visible = source && source.isActiveAndEnabled && glass.Source && glass.Source.gameObject.activeInHierarchy && wetness > 0.0005f;
            surface.enabled = visible && captured;
            if (!clock.Due(Time.timeAsDouble, visible && Time.timeScale > 0)) return;
            try
            {
                Capture(source);
                UpdateSurface(source);
                captured = true; surface.enabled = true;
            }
            catch (Exception error)
            {
                failed = true; surface.enabled = false;
                warning("Windshield camera refraction disabled; retaining tint: " + error.Message);
            }
        }

        private void Capture(Camera source)
        {
            int width = settings.Width.Value, height = WindshieldRefractionMath.CaptureHeight(width, source.aspect);
            if (!image || image.width != width || image.height != height)
            {
                if (image) { image.Release(); Object.Destroy(image); }
                image = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                    { name = "SlapPack windshield world capture", antiAliasing = 1, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!image.Create()) throw new InvalidOperationException("Could not allocate the windshield capture texture.");
                material.SetTexture("_BaseMap", image);
            }
            capture.CopyFrom(source); capture.enabled = false;
            capture.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            capture.targetTexture = image; capture.rect = new Rect(0, 0, 1, 1); capture.aspect = source.aspect;
            capture.projectionMatrix = source.projectionMatrix;
            capture.allowMSAA = false; capture.allowHDR = false;
            UniversalAdditionalCameraData sourceData = source.GetComponent<UniversalAdditionalCameraData>();
            if (sourceData)
            {
                if (RendererIndex != null) cameraData.SetRenderer((int)RendererIndex.GetValue(sourceData));
                if (cameraData.scriptableRenderer != sourceData.scriptableRenderer)
                    throw new NotSupportedException("Could not match the cockpit world camera's URP renderer.");
            }
            cameraData.renderType = CameraRenderType.Base; cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false; cameraData.requiresColorTexture = false; cameraData.requiresDepthTexture = false;
            request.destination = image;
            if (!RenderPipeline.SupportsRenderRequest(capture, request)) throw new NotSupportedException("The active renderer does not support a standalone URP camera request.");
            try
            {
                for (int i = 0; i < hidden.Length; i++)
                    if (hidden[i]) { previousVisibility[i] = hidden[i].enabled; hidden[i].enabled = false; }
                RenderPipeline.SubmitRenderRequest(capture, request);
            }
            finally
            {
                for (int i = 0; i < hidden.Length; i++) if (hidden[i]) hidden[i].enabled = previousVisibility[i];
            }
        }

        private void UpdateSurface(Camera source)
        {
            Matrix4x4 projection = source.projectionMatrix * source.worldToCameraMatrix * car.transform.localToWorldMatrix;
            float phase = Time.time * 1.7f, a = Mathf.Cos(phase), b = Mathf.Sin(phase);
            for (int i = 0; i < positions.Length; i++)
            {
                int x = Mathf.Clamp((int)((samples[i].X - field.MinX) / field.SpanX * WindshieldWeatherState.Width), 0, WindshieldWeatherState.Width - 1);
                int y = Mathf.Clamp((int)((samples[i].Y - field.MinY) / field.SpanY * WindshieldWeatherState.Height), 0, WindshieldWeatherState.Height - 1);
                int k = y * WindshieldWeatherState.Width + x;
                float water = field.Water[k];
                float gx = field.Water[y * WindshieldWeatherState.Width + Mathf.Max(0, x - 1)] - field.Water[y * WindshieldWeatherState.Width + Mathf.Min(WindshieldWeatherState.Width - 1, x + 1)];
                float gy = field.Water[Mathf.Max(0, y - 1) * WindshieldWeatherState.Width + x] - field.Water[Mathf.Min(WindshieldWeatherState.Height - 1, y + 1) * WindshieldWeatherState.Width + x];
                PanePoint offset = WindshieldRefractionMath.Offset(water, gx, gy, waves[i].x * a + waves[i].y * b, waves[i].y * a - waves[i].x * b, settings.Strength.Value);
                Vector3 screen = projection.MultiplyPoint(positions[i]);
                float v = Mathf.Clamp01(screen.y * 0.5f + 0.5f + offset.Y);
                uv[i] = new Vector2(Mathf.Clamp01(screen.x * 0.5f + 0.5f + offset.X), settings.FlipY.Value ? 1 - v : v);
                colors[i] = new Color32(255, 255, 255, (byte)(WindshieldWeatherOptics.Opacity(water, 0) * 255));
            }
            mesh.uv = uv; mesh.colors32 = colors;
        }

        private void OnDestroy()
        {
            if (capture) capture.targetTexture = null;
            if (image) { image.Release(); Object.Destroy(image); image = null; }
        }
    }
}
