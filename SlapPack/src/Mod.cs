using BepInEx.Configuration;
using HalfGripTools;
using UnityEngine;

namespace HalfGripSlapPack
{
    public sealed class Mod : HalfGripMod
    {
        private GameObject host;

        public override void Load(ModContext ctx)
        {
            host = new GameObject("SlapPack.Mirrors");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<MirrorInstaller>().Initialize(ctx);
        }

        public override void Unload()
        {
            if (!host) return;
            host.GetComponent<MirrorInstaller>().RemoveMirror();
            Object.Destroy(host);
            host = null;
        }
    }

    internal sealed class MirrorSettings
    {
        internal readonly ConfigEntry<float> Sensitivity, YawLimit, PitchLimit, Yaw, Pitch;
        private readonly ConfigFile config;

        internal MirrorSettings(ConfigFile config)
        {
            this.config = config;
            Sensitivity = config.Bind("Mirror", "Sensitivity", 8f,
                new ConfigDescription("Degrees per unit of cockpit hand movement.", new AcceptableValueRange<float>(0.1f, 40f)));
            YawLimit = config.Bind("Mirror", "HorizontalLimit", 30f,
                new ConfigDescription("Maximum horizontal adjustment in degrees.", new AcceptableValueRange<float>(1f, 60f)));
            PitchLimit = config.Bind("Mirror", "VerticalLimit", 20f,
                new ConfigDescription("Maximum vertical adjustment in degrees.", new AcceptableValueRange<float>(1f, 45f)));
            Yaw = config.Bind("Mirror", "SavedYaw", 0f, "Last horizontal adjustment in degrees.");
            Pitch = config.Bind("Mirror", "SavedPitch", 0f, "Last vertical adjustment in degrees.");
        }

        internal void Save(MirrorAdjustment adjustment)
        {
            if (Yaw.Value == adjustment.Yaw && Pitch.Value == adjustment.Pitch) return;
            Yaw.Value = adjustment.Yaw;
            Pitch.Value = adjustment.Pitch;
            config.Save();
        }
    }
}
