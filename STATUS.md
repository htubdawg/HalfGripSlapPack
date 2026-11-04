# SlapPack handover — 0.8.2

Weather work is isolated on `test/windshield-weather`, based on the existing `test/windshield-wipers` controls. No branches are merged by this work. The review PR is a draft for inspection/playtesting; the branch stack is intentional under the user's instruction.

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
