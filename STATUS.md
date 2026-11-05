# Maintenance update 0.8.9 — author credit and clean mod package

Corrects SlapPack/mod.json author to `Codex - Oretsej`, retaining the creator credit used by the other mods while naming the assistant actually used for Slap Pack. Adds the exact established credit to CLAUDE.md so future work preserves it.

Moves the compile-check project from SlapPack/SlapPack.csproj to repository-root SlapPack.csproj. Explicit source includes restrict compilation to SlapPack/src/**/*.cs; the default HalfGripRefs path and build documentation follow the move. Build outputs are generated outside the installable mod directory. SlapPack/ now contains only mod.json and runtime src/.

Manifest, README and project version are 0.8.9. Compile check passes with zero warnings/errors; all 159 existing model/control tests pass. Package layout and manifest/project/README version consistency are checked. No gameplay/rendering code changed.

---

# Test update 0.8.7 — default wiper stalk angle

Changes the new-config default for [Wipers] StalkUpAngle from 18 to 24 degrees, as requested. Existing saved config values are preserved; set StalkUpAngle = 24 explicitly to apply it to an existing config. Manifest, README and development project version are 0.8.7. The existing PR to main includes this update.

Validation: compile check passes with zero warnings/errors; all 159 model/control tests pass. No weather or mirror behavior is changed. Live stalk placement/shifter clearance at the new default still needs an in-game check.

---

# Test update 0.8.6 — stronger rear weather and mirror visibility checks

The user reports improved side streaks but weak rear activity and requests a look closer to the front windshield, including visibility in the mirror. This update adds a shallow, spatially varying rain layer with continuous natural drainage/drying, stronger individual beads and broader trails. The rear is no longer reduced at every stage: impact rate, head probability, active-head capacity, trail width and flow are increased. Both surfaces retain bounded water and natural clearing without wipers; rear tangential flow remains slower than side flow.

- At most 14 water heads per patch. Side/rear rain rates are 24/20 times precipitation exposure; rear flow uses 0.060 gravity projection and 0.0008 relative-air coupling, compared with 0.10/0.0045 on the sides.
- Combined local opacity caps are 40% side / 36% rear. Wet detail is stronger, while the shallow base layer remains bounded by drying. Snow retains its separate light, edge-biased layer. No scene-copy/refraction pass is added to these surfaces; the windshield optics remain as before.
- Existing point filtering, moving-window clipping and native-renderer-independent cockpit visibility remain. Manifest, README and development project version are all 0.8.6. AGENTS.md / CLAUDE.md were read before changes.

Mirror verification: inspected the existing RearViewCamera path and stock scene reference. Overlays are two-sided ordinary transparent world geometry, not a cockpit-only pass. They can render into the mirror's existing target. Source activation is used instead of the native glass renderer flag. Owned overlays now choose a layer included by both actual camera masks, rechecking immediately before URP camera culling, without changing either camera or source layer. A missing common layer emits a warning. Tests verify the common-layer fallback and incompatible masks.

With Diagnostics.VerboseLogging=true, five-second mirror-render callbacks report active rear patches, layer-included patches, wet source-triangle samples projected inside the actual mirror frustum (including lowered-glass clipping and near/far planes), and render-target dimensions. These are CPU eligibility/projection checks; they do not prove GPU fragments survived depth occlusion or appeared in the final mirror image. No live Unity game runtime is available here, so final pixel-level mirror visibility remains a playtest requirement.

Validation: compile check passes with zero warnings/errors; all 159 model/control tests pass. Vanilla side sample: longest connected trail 26 texels, 17.7% of samples above 8% opacity, mean opacity 4.8%. Vanilla rear sample: longest trail 12 texels, 12.5% above 8%, mean opacity 3.8%. Ten-minute stationary/moving storm tests enforce local caps and mean opacity below 14%. These are deterministic CPU field measurements, not in-game screenshots or performance measurements.

Runtime checks: install 0.8.6 on test/side-rear-weather; inspect rear weather directly and in the mirror in stationary rain and at speed, both stock bodies, mirror adjustment/Smart Mirror and IncludeCarInterior on/off, camera transitions, snow and drying. Check diagnostic wet samples > 0 when the mirror aims through wet rear glass; visually confirm depth sorting/occlusion and rendering cost. Main remains unchanged.

---

# Test update 0.8.5 — visible runoff streaks

The user reported that 0.8.4 droplets were barely visible. Bilinear water transport spread isolated small splats into faint patches; it did not retain a distinct runoff path. 0.8.5 adds bounded moving water heads that rasterize continuous narrow trails onto the glass. Those trails fade in place rather than diffusing with the bead field.

- Side heads follow stronger gravity and signed tangential airflow, leaving longer streaks while parked and slanted trails at speed. Rear glass retains fewer, slower heads and shorter trails.
- Brighter highlights and dark rims improve contrast against scenery. Rain impact density is increased. Combined local opacity caps are now 32% side / 24% rear, restricted to wet details; no blanket film is added. At most 12 side / 8 rear heads are active per patch.
- Natural drying, short trail lifetime, edge-biased snow, crank clipping and the 0.8.4 cockpit-visibility correction remain. Snow-only weather creates no liquid trails. No new camera, refraction pass or shared asset mutation is introduced.
- Root AGENTS.md explicitly directs Codex to read and follow CLAUDE.md as the shared repository instructions. Manifest, README and development project all identify this test build as 0.8.5.

Validation: compile passes with zero warnings/errors; all 155 model/control tests pass. New checks cover connected streak length/visible coverage in vanilla rain, ten-minute moving downpours, signed airflow reversal, drying of trails, open apertures, snow-only behavior and shorter rear paths. The vanilla side simulation produced a longest connected trail of 21 texels on the 64×32 grid, about 6.7% of samples above 8% opacity and about 1.7% mean opacity. Tested prolonged moving storms stay below 6% mean opacity with the local caps enforced. These are simulation measurements, not screenshots or in-game visibility claims.

Runtime checks: install 0.8.5 from test/side-rear-weather and check stationary rain, acceleration, reversing and crosswind; compare side and rear streak length/contrast; lower/raise each window; inspect both bodies, mirror view, camera swaps and snow/dry transitions. Measure Unity frame time and confirm that stronger localized detail remains comfortable. Main remains unchanged.

---

# Test update 0.8.4 — cockpit visibility and version identification

The initial side/rear test commit retained manifest 0.8.3 and incorrectly gated the precipitation renderer on `Source.enabled`. Decompiling HalfGripRefs 0.419 `CameraSwitch.UpdateCameraView` confirms the game calls `SetWindshields(false)` in cockpit view, disabling the original glass MeshRenderers while leaving their GameObjects active. As a result the new overlays were hidden from inside.

0.8.4 gates the owned weather renderer on source GameObject activation and window opening only, matching the existing windshield overlay. The stock renderer flag, original materials and camera settings remain unchanged. Per-pixel opacity caps, sparse deposition, natural clearing and moving-window clipping are retained.

Manifest, README and development assembly version are now 0.8.4. Future player-facing test updates must bump them together. Confirm `Loaded Slap Pack 0.8.4` or mod-manager version 0.8.4 after installation. This update is only on `test/side-rear-weather`; main is unchanged.

Validation: compile check passes with zero warnings/errors; all 149 model/control tests pass. Static inspection confirms the former gate contradicted the game's cockpit visibility behavior. Live Unity/GPU appearance, both bodies and mirror/camera transitions still need a playtest; no in-game visual verification is claimed.

---

# Side/rear window weather — implementation for playtesting

Work is on `test/side-rear-weather`, based on the GitHub main snapshot retrieved on November 4, 2026. Main and the earlier wiper test branch remain unchanged; this feature is prepared for playtesting on the test branch.

- `[Windows] WeatherEffects = true` adds precipitation to active side, quarter and rear glass independently of the wiper/crank enable switches. Uses the existing vanilla/Open World public weather source once per weather tick.
- Coplanar connected patches are discovered from stock glass triangles in car coordinates, excluding front glass. Opposite-winding duplicate faces are removed. Owned meshes, point-filtered 64×32 textures and materials follow source body activation; originals remain unchanged.
- Side glass receives sparse droplets, gravity runoff and signed airflow streaks. Sheltered rear glass receives fewer, slower-moving beads; normal incidence increases exposure when reversing or in wind. No blanket film or wiper clearing is applied.
- Snow consists of sparse patches biased toward actual aperture edges. Water dries and snow sheds naturally even during storms. Combined local added opacity cannot exceed 20% side / 16% rear; this is independent of windshield RainObscuration. The windshield simulation/refraction remains unchanged.
- Crank-controlled glass shares its open fraction with weather. Every source triangle moves downward and clips to the fixed opening, with UVs anchored to the glass. Hidden glass receives no fresh impacts; fully lowered glass has no weather geometry. Fixed quarter/rear panes remain in place.
- Runs at up to 20 Hz. Rebuilds geometry only on window travel, rebinds on car/body/mesh changes and cleans up on scene changes, disable and unload. Adds no cameras or scene-copy work.

Validation: compile against HalfGripRefs 0.419 / Unity 6000.3.22f1 and HalfGripTools 1.0.6: zero warnings/errors. All 149 model/control tests pass (138 existing plus 11 new). New tests cover ten-minute stationary mixed storms and per-pixel/mean opacity, natural drying/shedding, rear shelter, incidence, signed transport, edge snow, fully/partly open glass, invalid inputs and sloping-aperture clipping. These checks do not execute Unity/GPU rendering.

Runtime checks still required: both stock bodies, all side/quarter/rear panes, mirror view, shader alpha/brightness and depth sorting, prolonged rain/snow while parked/driving/reversing, crosswind, independently lowering/raising each door during weather, dry transitions, camera/body swaps, pause/resume, scene changes and mod unload. Check with window cranks disabled, wipers disabled and optional Open World weather. Confirm that visual density is noticeable but comfortable and profile the extra overlays in-game.

---

# SlapPack handover — 0.8.3

The user authorized merging the completed feature branch into main. PR #2 targets `main` directly, incorporating the stacked signal/wiper/weather work without updating the wiper test branch. Release 0.8.3 changes the default mirror FOV to 16°, retaining existing saved config values, and replaces the player README with features, controls, simplified installer/manual steps and common settings. Build/rendering/diagnostic details are in `docs/development.md`.

## Performance pass (0.8.2)

The user confirmed the 0.8.1 appearance. Preserve all optical/simulation settings, density and same-frame scene-copy behavior.
Cached field-neighbor stencils remove repeated coordinate conversions/clamps. Both blades share sweep trigonometry and skip identical
local geometry while parked; washer animation remains independent. An owned generated mesh uses separate static position/normal and
dynamic UV/color streams without format compression, preserving indices and bounds. Guarded layout setup retains the previous upload
path if unsupported. Projected UVs still update every visible rendered frame; an upload is skipped only on exact equality, with pending
changes retained while off-screen.

Baseline comparison: 13,041 offset/color samples plus wet ordering and 57,176 wiper points are bit-identical. Five-run CPU-only medians:
400 field refreshes 232.26 → 136.71 ms; 100,000 blade-point frames 121.26 → 23.05 ms. This excludes Unity native uploads, simulation,
RenderGraph and GPU work; it does not measure game FPS. Native stream layout and live frame times still need playtesting. Verbose logs
report original/optimized upload strides when setup changes the layout. No Open World API behavior changed.

## Current implementation

- The existing `car.fpvCamera` queues a RenderGraph pass after opaque objects and sky. A point-sampled built-in Blitter downsample feeds a dependent water-mesh draw before transparent rain/cockpit/UI rendering.
- No auxiliary camera, render request, renderer-index reflection, capture timer, manual flip config, persistent scene image, renderer-feature mutation or CPU image readback remains.
- The graph owns the temporary scene texture. Copy/draw resources and depth are explicitly declared; callbacks are removed on disable/rebuild/unload. Missing resources/materials/graph execution retain tint.
- The 160×80 water field drives a grid-resolution mesh through cached water/relief/impact gradients. Relief survives the water cap and short flash, advects with water, relaxes gradually, and is cleared by the blades. Point-visible offsets are quantized/cached in copy texels. Camera projection updates each rendered frame; no free-running wave animation remains. Visible wet samples gate work. The simulation remains capped at 20 Hz.
- Scene copy and weather overlay are point filtered. Refraction retains at least half of normal water tint, plus normal snow opacity. A loaded compatible stock transparent particle material is required; its keywords are preserved.
- Smart Mirror is suspended on first manual movement/reset, rather than attachment. Until then, SlapPack does not override auto-aim rotation. Only components actually suspended by SlapPack are re-enabled on unload.
- Routine logs use the verbose gate. The existing explicitly enabled geometry-report diagnostic still prints its requested output location.

## Validation

Compiled against HalfGripRefs 0.419 / Unity 6000.3.22f1 and HalfGripTools 1.0.6. Model/control tests cover field-driven offsets, visible-area gating, orientation scale/bias, tint floor, field revisions, buildup, wiping, runoff and existing controls. The final compile check passes with zero warnings/errors, and all 138 model/control tests pass. These checks do not execute the GPU or live Smart Mirror integration.

The supplied URP assembly's render loop executes RenderGraph; compatibility `Execute` methods are stubs. The new pass implements `RecordRenderGraph` and detects a missing execution rather than showing an old image.

## Runtime checks still required

Unity/GPU execution, image orientation on the player's backend, stock material eligibility/vertex alpha, postprocess color matching, depth/occlusion, dry/off-screen gating, camera/body transitions and cleanup need a live game test. Compare frame times with refraction off/on at 320 and 640, in vanilla and Open World. Test Helicopter integration and Smart Mirror attach/grab/adjust/reset/unload. No measured game-FPS improvement is claimed.

## Public Open World integration

The user-provided `claude/nifty-babbage-6o4kay` branch (0.24.0+) publishes WeatherView v1. SlapPack binds its public version getter and TryGetLocalWeather method to delegates; no private weather fields, reflected argument arrays, height corrections or rain/snow recomputation remain. It retries discovery for load order and falls back to vanilla on an inactive/absent/unsupported API. No Open World code or branch was changed or merged.

The integration harness compiles the actual upstream WeatherView.cs linked from that branch and stubs only Origin.Active. It verifies mixed precipitation, wind, activation/origin transitions, clear/restart and 10,000 allocation-free delegate reads. This proves the public contract binding, not live game rendering. See docs/public-weather-api.md for the reproducible command.

## New diagnostic workflow

Enable VerboseLogging and RefractionEnabled for the next test. Five-second snapshots show submitted callback/queue/graph/copy/draw counts, visible wet samples and offsets measured in copy texels. RefractionDebugTint optionally draws the wet mask in solid magenta, independent of scene alpha, to test visible mesh/shader output. Command counts do not prove rasterized pixels. The user reports the relief/texel result looks good; GPU visibility counters remain diagnostics, not a profiling measurement.

The shader path is mesh-based; it does not provide a new per-pixel normal-map shader. Finer normals now follow the water-grid resolution, with corresponding projection/vertex cost to measure in-game.
