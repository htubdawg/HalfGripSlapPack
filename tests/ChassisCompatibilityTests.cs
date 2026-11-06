using System;
using System.Collections.Generic;
using HalfGripSlapPack;
using UnityEngine;

/// <summary>Runs the actual chassis gate against small catalog/loadout stubs, without Unity rendering.</summary>
internal static class ChassisCompatibilityTests
{
	private static readonly ChassisUpgrade original = new ChassisUpgrade { id = "aero_1" };
	private static readonly ChassisUpgrade helicopter = new ChassisUpgrade { id = "helicopter.heli", hideFlags = HideFlags.DontSave };
	private static readonly ChassisUpgrade dacia = new ChassisUpgrade { id = "daciabody.legendary", hideFlags = HideFlags.DontSave };

	private static void Reset()
	{
		ItemCatalog.Instance = new ItemCatalog { items = new List<UpgradeComponent> { original } };
		PlayerLoadout.Known = true;
		PlayerLoadout.Equipped = new List<UpgradeComponent>();
		ChassisCompatibility.Initialize(message => throw new Exception(message));
	}

	private static void Expect(bool expected)
	{
		if (ChassisCompatibility.AllowsAdditions != expected) throw new Exception("Unexpected chassis eligibility.");
	}

	internal static void Run(Action<string, Action> check)
	{
		check("Chassis gate waits for known loadouts and accepts the unmodified base car", () =>
		{
			Reset(); PlayerLoadout.Known = false; Expect(false);
			PlayerLoadout.Known = true; Expect(true);
		});
		check("Stock chassis IDs remain supported in copied equipped items", () =>
		{
			Reset(); PlayerLoadout.Equipped.Add(new ChassisUpgrade { id = original.id, hideFlags = HideFlags.DontSave }); Expect(true);
		});
		check("Helicopter and Dacia chassis exclude all Slap Pack additions", () =>
		{
			Reset(); PlayerLoadout.Equipped.Add(helicopter); Expect(false);
			PlayerLoadout.Equipped[0] = dacia; Expect(false);
		});
		check("Unrecognized future chassis are excluded even with a plain ID", () =>
		{
			Reset(); PlayerLoadout.Equipped.Add(new ChassisUpgrade { id = "custombody" }); Expect(false);
		});
		check("Catalog injection cannot turn custom chassis into supported stock", () =>
		{
			Reset(); ItemCatalog.Instance.items.Add(dacia);
			PlayerLoadout.Equipped.Add(dacia); Expect(false);
			// A reload also excludes the runtime clones HalfGripTools creates.
			ChassisCompatibility.Initialize(message => throw new Exception(message)); Expect(false);
		});
		check("Chassis refits disable and restore support without a scene change", () =>
		{
			Reset(); PlayerLoadout.Equipped.Add(original); Expect(true);
			PlayerLoadout.Equipped[0] = helicopter; Expect(false);
			PlayerLoadout.Equipped[0] = original; Expect(true);
			PlayerLoadout.Equipped.Clear(); Expect(true);
		});
		check("Non-chassis mod items are allowed; every chassis entry is checked", () =>
		{
			Reset(); PlayerLoadout.Equipped.Add(new UpgradeComponent { id = "mod.gun" });
			PlayerLoadout.Equipped.Add(null); PlayerLoadout.Equipped.Add(original); Expect(true);
			PlayerLoadout.Equipped.Add(new UpgradeComponent { id = "mod.body", upgradeCategory = UpgradeCategory.CHASSIS }); Expect(false);
		});
		check("Unavailable original catalog disables support and warns once at initialization", () =>
		{
			Reset(); ItemCatalog.Instance = null;
			int warnings = 0; ChassisCompatibility.Initialize(message => warnings++); Expect(false);
			if (warnings != 1) throw new Exception("Missing catalog warning was not emitted.");
			Reset();
		});
	}
}

// Only the members read by ChassisCompatibility are stubbed. No Unity/GPU or
// installer cleanup behavior is simulated by these tests.
namespace UnityEngine
{
	[Flags]
	internal enum HideFlags { None = 0, DontSave = 52 }
	internal class Object
	{
		public HideFlags hideFlags;
		public static implicit operator bool(Object value) => value != null;
	}
}

internal enum UpgradeCategory { GUN, CHASSIS }
internal class UpgradeComponent : UnityEngine.Object
{
	public string id;
	public UpgradeCategory upgradeCategory;
}
internal sealed class ChassisUpgrade : UpgradeComponent { }
internal sealed class ItemCatalog : UnityEngine.Object
{
	public static ItemCatalog Instance;
	public List<UpgradeComponent> items;
}
internal static class PlayerLoadout
{
	public static bool Known;
	public static List<UpgradeComponent> Equipped;
}
