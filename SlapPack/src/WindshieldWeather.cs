using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
    internal sealed class WindshieldWeather
    {
        private readonly WindshieldWeatherState field;
        private readonly WiperWindshield glass;
        private readonly Texture2D texture;
        private readonly Color32[] pixels = new Color32[WindshieldWeatherState.Width * WindshieldWeatherState.Height];
        private readonly WindshieldWeatherSource source;
        private readonly CarControllerNew car;
        private readonly MeshRenderer renderer;
        private float nextUpload;

        internal WindshieldWeather(CarControllerNew car, WiperWindshield glass, List<Object> owned, Action<string> warning)
        {
            this.car = car; this.glass = glass;
            field = new WindshieldWeatherState(glass.Opening);
            source = new WindshieldWeatherSource(warning);
            texture = new Texture2D(WindshieldWeatherState.Width, WindshieldWeatherState.Height, TextureFormat.RGBA32, false)
                { name = "SlapPack windshield water/slush", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            owned.Add(texture);
            texture.SetPixels32(pixels); texture.Apply(false, false);
            Material material = WindowMaterials.Transparent(Color.white, texture, owned);
            material.name = "SlapPack windshield precipitation";
            var go = new GameObject("SlapPack.WindshieldWeather"); owned.Add(go);
            go.layer = glass.Source.gameObject.layer; go.transform.SetParent(car.transform, false);
            Mesh mesh = glass.WeatherMesh(field); owned.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        internal WindshieldPrecipitation Read() => source.Read(car);

        internal void Step(float dt, WindshieldPrecipitation weather, float previous, float current, bool washing)
        {
            Vector3 velocity = car.rb ? car.rb.linearVelocity : Vector3.zero;
            Vector3 relative = car.transform.InverseTransformDirection(weather.Wind - velocity);
            // Project gravity and air onto the real glass, so reversing and crosswind
            // reverse the appropriate streak direction. Normal incidence adds impacts.
            Vector3 flow = car.transform.InverseTransformDirection(Vector3.down) * 0.018f + relative * 0.0025f;
            PanePoint planeFlow = glass.ProjectVector(flow);
            float incidence = Mathf.Max(0, -Vector3.Dot(relative, glass.Normal));
            field.Deposit(dt, weather.Rain, weather.Snow, incidence, planeFlow.X, planeFlow.Y, washing);
            field.Wipe(previous, current);
        }

        internal void Draw()
        {
            renderer.enabled = glass.Source && glass.Source.gameObject.activeInHierarchy;
            if (Time.time < nextUpload) return;
            nextUpload = Time.time + 1f / 20;
            const int width = WindshieldWeatherState.Width;
            for (int k = 0; k < pixels.Length; k++)
            {
                if (!field.Inside[k]) continue;
                float water = field.Water[k], snow = field.Snow[k];
                // A bright leading rim, dark trailing rim and translucent center give
                // each deposit relief without shipping shaders or game textures.
                float gradient = (k >= width ? field.Water[k - width] : 0) - (k + width < pixels.Length ? field.Water[k + width] : 0);
                float shine = Mathf.Clamp01(0.46f + gradient * 1.4f + field.Impact[k] * 0.3f);
                float alpha = Mathf.Clamp01(water * (0.20f + Mathf.Abs(gradient) * 0.5f) + snow * 0.9f + field.Impact[k] * water * 0.12f);
                float white = snow / Mathf.Max(0.001f, snow + water);
                pixels[k] = new Color(Mathf.Lerp(shine * 0.8f, 0.88f, white), Mathf.Lerp(shine * 0.9f, 0.93f, white), Mathf.Lerp(shine, 0.96f, white), alpha);
            }
            texture.SetPixels32(pixels); texture.Apply(false, false);
        }
    }
}
