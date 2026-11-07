# Maintenance update 0.8.13 — game 0.419

jMods audit. `gameVersion` 0.419 (was 0.418); `tools` stays 1.0.6. Manifest, README and project version are 0.8.13.

Fixed:
- **Tutorial:** `ChassisCompatibility.AllowsAdditions` returned false while `PlayerLoadout.Known` was false, and the tutorial clears it (`TutorialGate.Play` calls `PlayerLoadout.Forget`), so every addition stayed off there. (The Playground is only opened by a dev command, which sets the loadout.) With an unknown loadout the game fits no chassis item (`LoadoutSpawner.ResolveLoadout` returns its editor list or nothing; `CarSkinController` uses the stock skin), so an unknown loadout now counts as the stock car. An unavailable original catalog still disables everything. Tests updated (one new).
- **Held-gun occlusion:** the per-frame scan reuses renderer and material lists (`GetComponentsInChildren(false, list)`, `GetSharedMaterials(list)`) instead of allocating while the glass is wet. The draw array handed to the render pass is still a fresh snapshot.
- **Weather source:** a bound WeatherView that reports inactive (outside Open World) is kept, not looked up and re-bound every two seconds. The missing-API warning now says to update Open World (0.24.0+) instead of pointing at the old API test branch.
- **Config comments:** `Windows.Enabled` no longer says "prototype"; `Wipers.Enabled` says it also removes windshield weather; `VerboseLogging` lists the mirror and held-gun diagnostics; `WriteGeometryReport` says where the report goes and that it holds game assets.
- **README:** Open World's WeatherView is in its releases since 0.24.0 (the test-branch link is gone, also from docs/public-weather-api.md); the settings table lists every setting (`StalkLength`, the turn-signal timing/cancel/stalk settings, `WriteGeometryReport`).

Checked against game 0.419: every member named by string still exists (`SmartMirror.baseLocalRotation`; `Grabber.Update`, `LateUpdate`, `BeginHeldLook`, `UpdateIndicators`, `TryInteract(bool)`; `GameInput.HandUp(int)`). The wiper-knob transpiler also rewrites `Grabber.Update`'s second `HandUp(0)` (the `suppressSweep` reset); that one runs with nothing held, so it behaves as before. `ItemCatalog.Instance` loads from Resources, so the chassis snapshot taken in `Mod.Load` is available before HalfGripTools injects items (that happens at scene load / `UpgradeMapper.Awake`). The rear camera's stock FOV in the 0.419 dumps is 28.07°, as the `MirrorFOV` comment says.

Validation: compile check against HalfGripRefs 0.419 / Unity 6000.3.22f1 passes with zero warnings/errors. The first pass used HalfGripTools 1.0.6 built from source; after the fixes, building HalfGripTools was refused in this session, so it ran against a stand-in `HalfGripTools` assembly with the `HalfGripMod`/`ModContext` members Slap Pack uses (signatures copied from HalfGripTools 1.0.6). All 171 tests pass.

Runtime checks: everything listed for 0.8.12 below, plus start the tutorial from the main menu and confirm the mirror, cranks, stalks, wipers and weather attach; then equip Helicopter/Dacia from the race menu and confirm they are still excluded.

---

# Test update 0.8.12 — held 3D gun depth occlusion

The player confirmed the prior transparent-queue change did not fix the held 3D weapon and supplied a screenshot. The aim model is distinct from the hanging cockpit sprite and its authored geometry can extend beyond the windshield. An opaque model rendered before the glass still loses the depth test to nearer glass, so queue 2999 alone cannot solve this case.

HeldGunWeatherOcclusion selects only active Gun.aimModel mesh/skinned renderers belonging to the current player's fpvCamera, with enabled renderers, included layers and solid/cutout materials. It queues a depth-only RenderGraph pass after sky, immediately before the existing refraction pass. The native material's enabled DepthOnly pass retains its cutout texture, material properties and vertex deformation. Transparent attachments, sprites, world weapons and inactive models are excluded. A missing/disabled depth pass warns once per material rather than masking a bounding rectangle.

The pass preserves projection X/Y/W, compressing only Z about the original near plane by 0.001. This retains the exact projected silhouette and near clipping while putting the depth silhouette in front of the glass. The gun's existing color remains untouched. Both the later refraction mesh and ordinary tint/slush overlays then fail depth inside that silhouette. It also works when refraction is disabled: attachment eligibility is based on actual owned windshield or side/rear weather, independent of refraction. Uses the existing camera and depth attachment, with explicit read/write dependency and scoped command-matrix restoration in finally. No camera properties, gun transforms/fire points, shared materials/meshes, shader keywords or light settings are changed, and there is no extra camera or previous-frame copy. Unload unsubscribes the callback before tearing down weather; custom chassis remain excluded.

Manifest, README and project version are 0.8.12. Compile check against HalfGripRefs 0.419 / Unity 6000.3.22f1 and HalfGripTools 1.0.6 passes with zero warnings/errors. All 170 tests pass, including new perspective/off-axis silhouette, near clipping and ordinary/reversed GPU depth conversion checks. Inspected the supplied URP RenderGraph camera setup to use the same CPU projection convention for SetViewProjectionMatrices. CPU tests and command submission do not establish final GPU pixels.

Runtime checks: reproduce the supplied held-shotgun overlap in rain and snow, refraction enabled and disabled; test all stock 3D guns, aiming at screen edges, recoil/spin, moving parts, release and gun swaps. Include custom guns exposing Gun.aimModel and enabled DepthOnly materials, both stock bodies, side glass, camera changes, foreground particles and any depth-based postprocessing. Verify water refraction near weapon edges does not smear sampled weapon color outward. Preserve ceiling sprites, crosshair and muzzle flashes. Custom materials without a native enabled depth pass are not supported by this mask and report a warning. Verbose diagnostics report selected submesh draws, graph records and depth submissions every five seconds; these do not prove rasterization. No live game runtime is available in this environment, so the screenshot's exact GPU result remains unverified.

---

# Test update 0.8.11 — exclude custom chassis

Every installer now checks a shared chassis gate before attachment and before retaining an existing binding. Mirror interaction, window cranks/moving glass, turn signals/relay/lamps, wipers/washer and all precipitation are removed when a custom chassis is equipped. Geometry diagnostics also skip custom chassis. Late-update weather/control work and refraction camera submission check eligibility too. Returning to an original chassis allows normal rebinding, including resetting the window installer's attempted-model latch so the same cabin can be fitted again.

During Mod.Load, before HalfGripTools 1.0.6 injects registered mod items, ChassisCompatibility captures original ChassisUpgrade IDs from ItemCatalog.Instance. Runtime DontSave clones are excluded from this snapshot (including on reload). It never refreshes from the later expanded catalog. Each known PlayerLoadout is checked against those IDs, including chassis-category items not subclassing ChassisUpgrade. Thus Helicopter (`helicopter.heli`), Dacia Body (`daciabody.legendary`) and other unrecognized chassis are excluded without dependencies on their assemblies, names, skins or cockpit hierarchy. Other categories of mod item remain supported. An unknown loadout waits; an unavailable/empty original catalog disables additions with a warning. This targets chassis items; unrelated mods that directly mutate original vehicle geometry without equipping a chassis item have no identity this gate can inspect.

Manifest, README and project version are 0.8.11. Compile check: zero warnings/errors. All 167 tests pass, including eight new tests linking the actual compatibility gate against narrow catalog/loadout/Unity object stubs. They cover base/stock/copy support, Helicopter/Dacia/future custom rejection, catalog injection and reload, refit transitions, non-chassis items, category-only chassis and unavailable metadata. They do not execute Unity installers or verify object destruction/restoration in-game.

Runtime checks: with both mods installed but not equipped, confirm stock additions still work. Equip Helicopter and Dacia before race start and during garage refits, confirm all Slap Pack visuals, interactions, relay audio and weather are absent, then refit stock and confirm reattachment. Repeat with both stock bodies, stock chassis upgrades, scene changes and mod disable/reload. Check original mirror hierarchy/camera state, glass materials and cockpit controls restore cleanly, particularly when changing chassis while holding a Slap Pack control. Retest the 0.8.10 gun/weather overlap correction below.

---

# Test update 0.8.10 — weather/gun draw order

The player reports windshield weather drawing over guns. Windshield and side/rear tint/slush materials previously used queue 3001, after ordinary transparent materials (3000). Because transparent gun surfaces can render without writing depth, this forces the weather over those surfaces even when the gun should be in front.

Both owned precipitation paths now use `WindowMaterials.Precipitation`, with queue `RenderQueue.Transparent - 1` (2999). This remains inside URP's transparent range, keeps alpha blending and the stock depth test with depth writes off, and draws before ordinary transparent guns/hands. The same-frame refraction pass already runs after sky and before transparents and is unchanged. Original game materials, gun transforms, camera settings, mirror layer selection and weather simulation remain unchanged.

Manifest, README and development project version are 0.8.10. Validation: compile against HalfGripRefs 0.419 / Unity 6000.3.22f1 and HalfGripTools 1.0.6 passes with zero warnings/errors; all 159 existing model/control tests pass. These checks do not execute Unity/GPU ordering or prove the visual fix.

Runtime checks: compare wet windshield overlap with guns resting, held, aimed, firing, reloading and spinning; include stock guns and optional gun mods. Check rain and snow with refraction on/off, look sideways through wet glass, inspect rear weather in the mirror and try both bodies. Confirm opaque weapon parts still occlude the glass according to depth. If an aimed model physically extends past the glass or uses a custom earlier render queue, this queue correction may not resolve that overlap; inspect that case in-game before changing weapon placement or rendering.

---

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
