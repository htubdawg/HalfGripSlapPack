using BepInEx.Configuration;
using BepInEx.Logging;

namespace HalfGripSlapPack
{
	/// <summary>Keeps routine diagnostics opt-in; warnings and errors remain visible.</summary>
	internal static class ModLog
	{
		private static ConfigEntry<bool> verbose;

		/// <summary>Binds the common log gate before any feature initializes.</summary>
		internal static void Initialize(ConfigFile config) => verbose = config.Bind("Diagnostics", "VerboseLogging", false, "Log routine feature attachment and integration details.");

		/// <summary>Emits routine information only when verbose diagnostics are enabled.</summary>
		internal static void Info(ManualLogSource log, string message)
		{
			if (verbose != null && verbose.Value) log.LogInfo(message);
		}
	}
}
