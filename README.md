# Slap Pack for Half Grip

**Version 0.8.7** · for Half Grip Demo 0.418 · needs [HalfGripTools](https://github.com/4l3x4nd3r0/HalfGripTools) **1.0.6 or newer**

More things to grab in your car: adjust the rear-view mirror, wind the windows down, use the turn signals, and clear rain and slush
with working windshield wipers. Rain builds up on the windshield and distorts the view until you wipe it away. Side and rear windows get lighter, self-clearing weather.

## In the cockpit

Use the game's normal hand/grab control.

| Control | How to use it |
|---|---|
| **Rear-view mirror** | Grab the casing and drag to aim it. Release to save the position. Right-click while hovering or holding to reset |
| **Window cranks** | Grab either door's crank and move your hand in circles. Clockwise lowers that window; counterclockwise raises it |
| **Turn-signal stalk** (left) | Grab the end and move down for left, up for right. A halfway tap gives three flashes; full travel latches it. Return to neutral to cancel |
| **Wiper speed sleeve** (right) | Hold right-click on the end and drag up/down through **0 / I / II / III**: off, intermittent, continuous, fast |
| **Washer stalk** (right) | Left-click and pull down to spray and wipe. Release stops the spray; a quick pull completes at least three full wipe cycles |

The mirror now defaults to a **16° field of view**. Its adjustment is saved between races. If you have the Smart Mirror upgrade,
auto-aim stays active until you manually move or reset the mirror.

The two windows move independently and start fully raised each race. Turn signals have dashboard arrows, exterior lamps and a relay
click, and cancel after you complete the indicated turn. The wipers finish their stroke before parking when switched off.

### Rain and snow

Version **0.8.7** sets the new-config wiper stalk angle to **24°** and includes a shallow wet layer and stronger beading/runoff, with much more activity on the rear glass. The overlays use a render layer included by both the cockpit and existing rear-view cameras. Confirm the mod manager or startup log shows **Slap Pack 0.8.7** after updating the test branch.

On the windshield, rain leaves droplets and running water; snow builds a wipeable slush layer. Impacts, airflow and car speed affect the buildup and runoff.
The blades clear their swept area, leaving water around the edges. Water bends the view through the glass using the current frame,
with point filtering to match the game's pixels.

All side windows (including quarter panes) and the rear glass get sparse rain beads and light snow patches. A thin, irregular wet layer connects the brighter beads and runoff without requiring a wiper. Side droplets run downward
and trail with airflow, leaving narrow, fading rivulets with bright and dark rims; the sheltered rear beads move more slowly. Snow favors the edges. These surfaces drain/dry and shed snow
naturally, with added opacity capped at 40% per pixel on the sides and 36% on the rear, since there is no rear wiper or defroster.
Door-window deposits move with the crank-operated glass and disappear below the sill when lowered.

Vanilla rain and snow work on their own. **Continuous mode (II)** is intended for vanilla rain; use intermittent for lighter weather
and fast for heavier rain. Mode selection stays manual.

[Open World](https://github.com/htubdawg/HalfGripOpenWorld) weather is supported through its public **WeatherView v1** API, including
variable rain/snow intensity and world wind. That API is currently supplied by its
[0.24.0+ API test branch](https://github.com/htubdawg/HalfGripOpenWorld/tree/claude/nifty-babbage-6o4kay).
Older builds without the API use vanilla weather flags and cannot provide Open World's variable intensity.

## Install

You need Windows and the game on Steam.

### Easy way: the installer

1. Download `HalfGrip-Mods-Installer.exe` from the [HalfGripMods Releases page](https://github.com/4l3x4nd3r0/HalfGripMods/releases)
   (the newest release called **installer**) and open it.
2. Close the game. In the installer, press **+ ADD**, paste `https://github.com/htubdawg/HalfGripSlapPack`, then press **ADD**.
3. Find **Slap Pack** and press **INSTALL**.
4. Start the game from Steam. The first start compiles the mod, which takes a few seconds. The main-menu mod manager should list **Slap Pack**.

The installer sets up BepInEx and HalfGripTools if they are missing. Open it again to update; **CHECK NOW** checks for updates straight away.
More about installing and removing mods is in the [HalfGripMods README](https://github.com/4l3x4nd3r0/HalfGripMods#install).

### Manual steps

1. Install BepInEx and [HalfGripTools](https://github.com/4l3x4nd3r0/HalfGripTools) **1.0.6+** if needed. Steps 1 and 2 of the
   [HalfGripTools README](https://github.com/4l3x4nd3r0/HalfGripTools#install) cover both.
2. Download this repository (**Code > Download ZIP**) and unzip it.
3. Delete any older `SlapPack` folder in `<game>\BepInEx\HalfGripMods\`, then copy the **`SlapPack`** folder from the download into it.
4. Start the game and wait for the main menu. The mod manager should list **Slap Pack**.

> [!IMPORTANT]
> **Copy only the `SlapPack` folder, not the whole download.** The ZIP contains `HalfGripSlapPack-main\`, with `SlapPack` inside it.
> `mod.json` must sit directly inside `HalfGripMods\SlapPack\`.

Correct layout:

```text
Half Grip Demo\
└── BepInEx\
    └── HalfGripMods\
        └── SlapPack\
            ├── mod.json
            └── src\
```

`<game>` is usually `C:\Program Files (x86)\Steam\steamapps\common\Half Grip Demo`.
You do not need the repository's tests, documentation, `.csproj`, `bin` or `obj` folders to play.

## Settings

Settings are written to `BepInEx\config\halfgrip.slappack.cfg` after the first launch. Close the game before editing it in Notepad,
then restart. Existing saved settings are kept when updating: to use the new mirror default, set `MirrorFOV = 16` under `[Mirror]`.

| Setting | Default | What it does |
|---|---|---|
| `Mirror.MirrorFOV` | `16` | Rear-view field of view in degrees (10–60). Lower values zoom in |
| `Mirror.IncludeCarInterior` | `true` | Includes the car's 3D cabin in the mirror view |
| `Mirror.Sensitivity` | `8` | How quickly the mirror moves while dragging |
| `Mirror.HorizontalLimit` / `VerticalLimit` | `30` / `20` | Maximum mirror adjustment in degrees |
| `Windows.Enabled` | `true` | Enables the two window cranks and moving glass |
| `Windows.WeatherEffects` | `true` | Light side/rear rain and snow; independent of crank and wiper settings |
| `Windows.TintOpacity` | `0.04` | Tint on raised side-window glass |
| `Windows.TurnsForFullTravel` | `3` | Crank turns from fully raised to fully lowered |
| `TurnSignals.Enabled` | `true` | Enables the signal stalk, arrows and exterior lamps |
| `TurnSignals.ClickSound` / `ClickVolume` | `true` / `0.35` | Relay sound and volume |
| `Wipers.Enabled` | `true` | Enables wipers and their controls |
| `Wipers.WeatherEffects` | `true` | Enables windshield rain/slush buildup and wiping |
| `Wipers.StalkUpAngle` | `24` | Upward neutral stalk angle in degrees. Existing saved values are retained |
| `Wipers.IntermittentPause` | `2.5` | Seconds parked between wipes in mode I |
| `Wipers.RainObscuration` | `1` | Strength of windshield water obscuration |
| `Wipers.RefractionEnabled` | `true` | Water distortion; disabling it keeps the tint overlay |
| `Wipers.RefractionCaptureWidth` | `320` | Scene-copy width (320–1280). Higher values give finer detail at more cost |
| `Wipers.RefractionStrength` | `0.008` | Strength of the distortion |
| `Diagnostics.VerboseLogging` | `false` | Detailed binding, weather-provider and rendering logs |
| `Diagnostics.RefractionDebugTint` | `false` | Magenta wet-mask diagnostic; leave off for normal play |

## Notes

- **Troubleshooting:** check `BepInEx\LogOutput.log` for `Loaded Slap Pack` and any warnings/errors. For weather problems, enable
  `Diagnostics.VerboseLogging`, restart, and share the log. [Development notes](docs/development.md) explain the diagnostics.
- **Multiplayer:** turn signals are local visuals and are not replicated to other players' cars.
- **Limitations:** windows change the visible glass, not bullet collision or sound. There is no persistent ice/frost, washer-fluid
  consumption, automatic rain sensing or hazard-light control. Unsupported door artwork is skipped with a warning.
- **Removing the mod:** close the game and delete `SlapPack` from `BepInEx\HalfGripMods\`, or use the installer.
- **Speedrun mode:** HalfGripTools does not load gameplay mods in speedrun mode.
- **For developers:** see [build, validation and rendering notes](docs/development.md) and the [public weather API contract](docs/public-weather-api.md).
