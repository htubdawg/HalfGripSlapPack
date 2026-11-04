using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HalfGripSlapPack
{
    internal struct WindshieldPrecipitation
    {
        internal float Rain, Snow;
        internal Vector3 Wind;
    }

    // Optional read-only adapter. No Open World assembly reference, patches, particle
    // mutation or dependence on graphics-budget emission rates.
    internal sealed class WindshieldWeatherSource
    {
        private WeatherController controller;
        private Component sky;
        private FieldInfo ready, rain, storms, lift, windAngle, windSpeed;
        private MethodInfo snowShare;
        private float nextScan;
        private bool warned;
        private readonly Action<string> warning;

        internal WindshieldWeatherSource(Action<string> warning) { this.warning = warning; }

        internal WindshieldPrecipitation Read(CarControllerNew car)
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 2;
                if (!controller) controller = UnityEngine.Object.FindFirstObjectByType<WeatherController>();
                if (!sky) FindSky(car);
            }
            if (sky is Behaviour activeSky && activeSky.isActiveAndEnabled)
            {
                try
                {
                    if ((bool)ready.GetValue(sky))
                    {
                        object storm = storms.GetValue(sky);
                        double height = car.transform.position.y - Convert.ToDouble(lift.GetValue(null));
                        float falling = WindshieldWeatherState.Unit(Convert.ToSingle(rain.GetValue(sky)));
                        if (falling < 0.02f) falling = 0; // Same cutoff as Sky.Step.
                        float share = WindshieldWeatherState.Unit(Convert.ToSingle(snowShare.Invoke(storm, new object[] { height })));
                        double angle = Convert.ToDouble(windAngle.GetValue(storm));
                        float speed = Convert.ToSingle(windSpeed.GetValue(storm));
                        return new WindshieldPrecipitation
                        {
                            Rain = falling * (1 - share), Snow = falling * share,
                            Wind = new Vector3((float)Math.Cos(angle) * speed, 0, (float)Math.Sin(angle) * speed)
                        };
                    }
                }
                catch (Exception error) { Warn("Open World weather could not be read; using vanilla precipitation: " + error.Message); }
            }
            WeatherProfile profile = controller ? controller.activeProfile : RunState.ActiveWeather;
            return new WindshieldPrecipitation { Rain = profile && profile.rain ? 0.5f : 0, Snow = profile && profile.snow ? 0.5f : 0 };
        }

        private void FindSky(CarControllerNew car)
        {
            Type type = AccessTools.TypeByName("HalfGripOpenWorld.Sky");
            if (type == null) return;
            // The optional mod may be compiled/loaded after Slap Pack; retry discovery.
            foreach (UnityEngine.Object candidate in UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None))
            {
                Component component = candidate as Component;
                if (!component || component.gameObject.scene != car.gameObject.scene) continue;
                try
                {
                    ready = AccessTools.Field(type, "ready"); rain = AccessTools.Field(type, "rain"); storms = AccessTools.Field(type, "storms");
                    Type stormType = storms?.FieldType;
                    snowShare = stormType == null ? null : AccessTools.Method(stormType, "SnowShare", new[] { typeof(double) });
                    windAngle = stormType == null ? null : AccessTools.Field(stormType, "WindAngle");
                    windSpeed = stormType == null ? null : AccessTools.Field(stormType, "WindSpeed");
                    Type network = type.Assembly.GetType("HalfGripOpenWorld.Network");
                    lift = network == null ? null : AccessTools.Field(network, "Lift");
                    if (ready == null || rain == null || storms == null || snowShare == null || windAngle == null || windSpeed == null || lift == null)
                        throw new MissingMemberException("Unsupported Sky/Storms interface");
                    sky = component;
                }
                catch (Exception error) { Warn("Open World weather adapter unavailable: " + error.Message); }
                return;
            }
        }

        private void Warn(string message) { if (!warned) { warned = true; warning(message); } }
    }
}
