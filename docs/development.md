# Slap Pack development

Player controls, installation and common settings are in the [README](../README.md). The release combines the mirror, window, signal,
wiper and weather work; the user authorized merging it into main. No Open World branch is changed or merged here.

## Build and check

Use the .NET 8 SDK, a HalfGripRefs checkout with private game assemblies, and a built HalfGripTools 1.0.6+ DLL.
From the repository root:

```bash
dotnet build SlapPack.csproj -c Release \
  -p:HalfGripRefsDir=/path/to/HalfGripRefs \
  -p:HalfGripToolsDll=/path/to/HalfGripTools.dll
dotnet run --project tests/MirrorAdjustment.Tests.csproj -c Release
```

HalfGripTools compiles `SlapPack/src` at game startup. The repository-root SlapPack.csproj is a development compile check and explicitly includes only SlapPack/src/**/*.cs. Its bin/obj output stays at the repository root; no development project or build output belongs inside the installable SlapPack folder.
Keep private game DLLs, assets and diagnostic texture reports outside the repository and distribution.

The compile check uses HalfGripRefs 0.419 / Unity 6000.3.22f1 and HalfGripTools 1.0.6. All 138 model/control tests pass. They cover mirror
adjustment and camera masks, window travel/clipping, signal state/audio/layout, wiper controls/geometry, field accumulation/wiping/runoff,
refraction math, visible-area gating, cached field neighbors and optional weather binding. The [API integration harness](public-weather-api.md)
links the actual upstream WeatherView.cs and checks the public contract, including allocation-free delegate polling.

These checks do not execute Unity's GPU rendering or prove live optional-mod compatibility. In-game checks include camera/body transitions,
orientation, depth/occlusion, material availability, pause/focus loss, washer cycles, Smart Mirror takeover/restoration and mod unload.

## Rendering and performance

Version 0.8.12 adds a depth-only foreground silhouette for the active `Gun.aimModel` 3D renderers. The native solid/cutout material's DepthOnly pass draws into the existing camera depth after sky and before refraction. Command projection X/Y/W are unchanged; Z is compressed about the near plane, preserving silhouette and near clipping. Command matrices are restored in finally; no gun transforms, shared assets or camera properties change. The existing gun color is retained and the foreground depth blocks both refraction and tint/slush overlays, including when refraction is off. The pass runs only for the player's world camera with owned weather; custom chassis remain excluded. Custom gun shaders need an enabled DepthOnly pass. Verbose five-second diagnostics report selected draws/graph records/depth submissions. Actual rasterization, edge sampling and depth-based postprocess interaction require a game test.

Version 0.8.11 restricts attachments to original chassis IDs captured from the item catalog during Mod.Load, before HalfGripTools injects mod items. Runtime DontSave clones are excluded. The live PlayerLoadout must be known, and every equipped chassis must match the original snapshot. All installers remove their owned additions when eligibility is lost and can rebind after a stock refit; window attachment retries reset on exclusion. This does not depend on custom cockpit names or whether other mods are installed. The test suite links the actual gate against catalog/loadout stubs; live refit cleanup and restoration still require a game test.

The existing `car.fpvCamera` enqueues a RenderGraph pass after opaque objects and sky. A point-filtered built-in Blitter downsample feeds
a dependent water-mesh draw before transparent rain/cockpit/UI rendering. Resources and read-only depth dependencies are explicit. There
is no second camera, render request, capture timer, previous-frame scene image, renderer-feature mutation or CPU image readback.
The supplied URP assembly executes RenderGraph; compatibility `Execute` stubs are insufficient.

The 160×80 water/slush simulation runs at up to 20 Hz. Persistent relief is independent of the water cap, advects with water, relaxes
gradually and is cleared by the blades. Gradients of water, relief and impacts drive field-resolution mesh offsets. Meaningful offsets
are quantized in copy texels for point filtering; tiny noise stays zero. Camera projection updates each rendered frame. Work is gated by
visible wet samples. Refraction retains at least half of the ordinary water tint, plus full snow opacity. Missing/unsupported rendering
retains the tint overlay. The material is cloned from a loaded compatible stock transparent particle variant, preserving its keywords.

Version 0.8.2 caches field-neighbor addresses, shares wiper trigonometry and skips unchanged parked blade geometry. Owned generated
refraction meshes separate static Float32 positions/normals from dynamic Float32 UVs and packed RGBA colors, preserving indices and
bounds. Layout guards retain the legacy upload path if unsupported. UV uploads skip only exact equality, retaining pending changes while
off-screen. No resolution, density, rate, point-filtering, optical or simulation settings were reduced.

The CPU-only 0.8.1 comparison found bit-identical offsets/colors/wet ordering for 13,041 samples and 57,176 identical wiper points.
Five-run medians: 400 field refreshes 232.26 → 136.71 ms (41.1% less); 100,000 moving-blade point frames 121.26 → 23.05 ms (81.0% less).
These exclude Unity native uploads, simulation, RenderGraph and GPU work, and do not measure overall game FPS. The user confirmed the
appearance before the optimization; native split-stream rendering and actual frame-time gains still need an in-game comparison.

## Refraction diagnostics

In 0.8.10, windshield and side/rear precipitation tint/slush materials use transparent queue 2999 instead of 3001. They draw before ordinary transparent gun/hand surfaces (3000), which may not write depth. Stock depth testing and alpha blending are retained. The refraction pass still precedes transparents. Verify overlap in-game with both refraction modes; the CPU model suite cannot validate GPU ordering, and gun geometry extending beyond the glass needs a separate runtime diagnosis.

For a targeted test, enable `VerboseLogging` under `[Diagnostics]`, retain `RefractionEnabled = true` under `[Wipers]`, and restart.
Five-second snapshots report callback/queue/record/copy/draw counts, last draw frame, copy size, visible wet samples and offsets in scene-copy
texels. Counts prove command submission, not rasterized pixels. Stream setup reports original/optimized upload strides when it changes the
layout. Weather-provider changes should show `Open World WeatherView v1` in an active compatible world.

`RefractionDebugTint = true` draws a solid magenta wet mask using a white source texture, independent of scene alpha, to test visible
mesh/material output. Return it and verbose logging to false for normal play. Compare frame times with refraction off/on at 320 and 640,
in vanilla and Open World, rather than relying only on average FPS. Also check Helicopter integration; no unknown camera is created.

## Window geometry diagnostics

Set `WriteGeometryReport = true` under `[Windows]`, start a race, and find `Window-crank geometry report written to` in the log.
Reports are stored under `BepInEx/SlapPack/WindowDiagnostics/<scene>-<timestamp>/` and include hierarchy, geometry and texture previews.
The diagnostic runs once per spawned player car and can pause the game briefly. Turn it off afterward. Reports contain private game
reference assets and must stay outside the repository and distribution. Shared meshes/materials and their read/write flags are not modified.

## Release 0.8.3

Changes the new-config mirror FOV from 22° to 16° and rewrites the player README around ProcTrack's features/install/settings structure.
Existing BepInEx values remain intact; updating users can set `[Mirror] MirrorFOV = 16` explicitly. The default change does not alter the
refraction or performance pass.


## Side/rear weather

`WindowWeatherInstaller` owns unwiped glass effects separately from `WiperInstaller`. `WindowWeatherSurface` selects connected coplanar side/rear triangles and retains their footprint; `WindowWeatherState` simulates sparse, bounded deposits with no base film or windshield clearing. Each patch has a 64×32 point-filtered texture, updated at up to 20 Hz. Source triangles are de-duplicated before two-sided rendering.

`WindowInstaller.MovingPane` maps matching source indices to the door's open fraction. Weather geometry moves with that glass, clips against its aperture and keeps UVs in glass coordinates; fresh deposition is restricted to the exposed part. The original glass meshes/materials remain unchanged. No refraction camera/pass is added for side or rear windows.

The expanded model suite has 149 passing tests. Live Unity rendering, mesh discovery on both bodies, mirror visibility and frame-time cost still require the runtime checks in STATUS.md.

### Test build 0.8.4 visibility correction

The game intentionally disables native glass MeshRenderers in cockpit view (`CameraSwitch.SetWindshields(false)`). Owned precipitation follows source GameObject activation, not the native renderer's enabled flag. Using that flag hid the initial side/rear effect in the cockpit. Manifest, README and development assembly version identify this correction as 0.8.4; live rendering remains to be verified.

### Test build 0.8.5 runoff trails

A bounded set of water heads stamps traveled segments into a fading 64×32 trail field. Side heads move with stronger projected gravity/airflow; rear heads remain slower and fewer. Trails remain fixed to the glass and decay instead of repeatedly undergoing bilinear advection, retaining visible streaks. Higher water contrast and localized opacity replace the faint isolated dots reported in 0.8.4. No scene sampling or extra camera is added.

The 155-test suite includes connected-streak, sustained-coverage, airflow-reversal and trail-clearing checks. The local caps are now 32% side / 24% rear; typical vanilla side mean opacity in the deterministic model is about 1.7%. Game appearance and native rendering cost still need playtesting.

### Test build 0.8.6 rear/mirror update

A thin spatially varying rain layer complements the discrete beads and fading trails; natural drying bounds it without wipers. Rear impact rate, bead probability, trail width and flow are increased. Local caps are 40% side / 36% rear; deterministic vanilla means are approximately 4.8% / 3.8%. This continues to use transparent world overlays rather than adding another scene-color copy.

`WindowWeatherLayer.Select` keeps owned overlays in the intersection of the cockpit and existing mirror camera masks, preferring their source layer. Only the owned overlay layer changes. A beginCameraRendering callback rechecks masks before culling and optionally reports wet rear-triangle samples in the mirror viewport and its render-target size. No auxiliary camera, source-layer edit or original-camera modification is introduced. Callbacks are removed on disable/unload.

The 159-test model suite includes rear visibility/coverage and mirror-layer selection. Diagnostics check CPU projection/layer eligibility; live mirror pixels, depth occlusion and Unity performance still require playtesting.
