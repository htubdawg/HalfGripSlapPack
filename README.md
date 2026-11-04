# HalfGripSlapPack
A collection of new things to interact with in your car.

Features: adjustable rear-view mirror, working window cranks, turn signals, and windshield wipers with rain/slush interaction.

## Experimental camera refraction — 0.7.0

On `test/windshield-weather` only; no merge into the wiper-control branch. Version 0.7.0 prototypes a **separate low-resolution camera**
for water distortion. It defaults to **640 pixels wide** (640×360 at 16:9) and **at most 15 captures/second**, configurable below. The camera
copies the cockpit world camera's current pose, projection, culling mask and clipping distances. It renders once per scheduled capture using
URP's standalone camera request, without a CPU image readback. Its capture remains on the GPU.

The existing URP particle unlit shader draws that image through a tessellated front-glass mesh. Wetness controls vertex alpha, while the
water gradient and animated ripples offset the sampling UVs. Ripples remain at saturation; wiping removes both the mask and distortion.
Rain's colored overlay is greatly reduced while refraction is available; snow/slush retains its opacity. The method approximates refraction
with a mesh and avoids depending on undocumented inputs of the game's speed-line shader. Native speed-line shader reuse is not implemented.

The helper camera is disabled between explicit captures and sleeps when the windshield is dry, while paused, or outside the cockpit.
The water overlay, refraction surface, own blades/washer jets and own speed-line renderer are temporarily hidden only during capture to avoid
feedback and duplicate effects; their visibility is restored in a `finally` block. The original cameras and their settings are unchanged.
New textures/camera/mesh/materials are removed with the weather surface on body changes or mod unload. Unsupported rendering requests or
allocation failures warn once and restore the tint effect.

Open World continues to provide precipitation as before. The capture inherits the main camera's current far clip each time, including
Open World's extended horizon. Its streamed/far terrain are ordinary world renderers; no Open World camera registration or patch is added.
The capture remains untagged so it does not replace `Camera.main`. These are source-level compatibility checks; live Open World rendering
still requires the playtest below.

```ini
[Wipers]
RefractionEnabled = true
RefractionCaptureWidth = 640
RefractionCaptureHz = 15
RefractionStrength = 0.008
RefractionFlipY = false
```

Restart after changing. Width allows 320–1280, rate 5–30 Hz, strength 0.001–0.02. Set `RefractionEnabled = false` to compare against 0.6.2-style
water tint. The existing `RainObscuration` controls the tint fallback/highlights; `RefractionStrength` controls the ripple sampling offset.
If the captured scenery is upside down on your graphics backend, set `RefractionFlipY = true` and restart.

**Tradeoffs to check in-game:** the captured world updates at the configured rate, so wet areas can lag or ghost while driving/looking around.
The helper renders world geometry again; low resolution reduces pixel work but does not eliminate CPU culling/draw submission. Its request
disables shadows, MSAA, HDR and postprocessing to keep cost down, so brightness/shadow differences from the normal view may be visible.
There is no measured Unity/GPU performance result here. Do not assume this is faster than sampling a scene-color copy.

Playtest vanilla and Open World in rain: verify bent scenery without gray saturation, clean wiped arcs, correct texture orientation and
alignment when turning/look controls/FOV change, cockpit/hand/HUD occlusion, distant terrain, pause/outside-camera behavior and mod unload.
Compare refraction off versus 640/15 and 320/15 at the same location; also try 640/30 for image latency versus cost. Watch for a
`Windshield camera refraction disabled` warning. Rendering, particle-shader vertex alpha, capture color matching and live performance need
this test; the automated tests exercise timing, dry/wiped masks, saturated ripples, offset bounds and capture aspect only.

## Windshield precipitation — 0.6.2

Version **0.6.2** limits runoff of water adhered to the windshield and removes speed-dependent evaporation. The previous version could
combine fast transport off the glass with extra drying, making a favorable heading too effective at clearing rain without the wipers.
Wind still changes streak direction and impact exposure, but its glass-plane transport is capped at 0.025 cockpit units/second. Ordinary
slow drying remains when precipitation stops. The change retains the 20 Hz field-update limit. A model regression checks eight headings
at three racing speeds (40/80/120 game units per second) under the same rain and 11-unit/s world wind, with wipers off.

The weather experiment stays on **`test/windshield-weather`**, separate from `test/windshield-wipers`; no merge is required.
Version 0.6.1 strengthens water obscuration and addresses the expensive per-frame field simulation reported during playtesting.
Rain now deposits a broad, slightly uneven water film as well as larger drops, so leaving the wipers off reduces visibility across the glass.
Water opacity rises with the film instead of staying near the original 20% ceiling. In the stationary vanilla model, 10 seconds with wipers
off produces roughly 72% mean overlay opacity; continuous wiping still maintains the swept area. This is a model check, not a visual guarantee.

The full water/slush field and texture upload now run at most **20 Hz**. Cheap motor samples collect the entire traversed range, including
reversals, before each field update. Drift interpolation weights are computed once per update instead of per pixel, and long hitches do not
queue field catch-up work. A 144 FPS CPU-only comparison over 10 simulated seconds reduced full-field passes from 1,440 to 200 and field CPU
time by about 93% on the cloud worker. This excludes Unity, rendering, texture uploads and the GPU; actual game FPS needs another playtest.

`[Wipers] RainObscuration = 1` is the new default. Increase it toward 3 for stronger water obscuration, or reduce it toward 0.5.
Restart after changing. `WeatherEffects = false` remains available for a comparison with the weather overlay disabled.

This implementation starts from `test/windshield-wipers` (0.5.3). Copy the supplied `SlapPack` folder over the existing
`BepInEx/HalfGripMods/SlapPack` folder and restart. HalfGripTools compiles the source; no private game assets or new dependency are shipped.

Rain creates small splash highlights and persistent water deposits. Snow creates pale, denser slush patches. Both build up over time,
remain after precipitation stops, and drift/drain along the glass under gravity and car-relative airflow. Slush moves more slowly than water.
Forward motion increases the rate of windshield impacts; reversing and crosswind change streak direction. This pass uses wipeable slush,
without persistent ice/frost. The overlay follows only the real front windshield triangles, including the center bend.

The actual rubber section of each blade clears both layers as it passes. Glass outside the rubber's reach retains buildup. Parked blades
do not clear the screen. Motor substeps record the swept path while deposition and clearing are batched into 20 Hz field updates, preserving
coverage through stroke reversals without full-grid work per motor sample. The mask may trail the blade by up to one field update, about 50 ms
at normal frame rates. The washer deposits water at its existing nozzle targets, which the normal washer wipe burst removes.

Selection stays **manual**: off / intermittent / continuous / fast, with the existing 2.5-second intermittent pause, 1.4-second normal
cycle and 0.8-second fast cycle. Faster modes reduce the time available for deposits to return between passes; they do not switch themselves.

- **Vanilla:** `WeatherController.activeProfile.rain/snow` selects precipitation at a fixed normalized intensity of **0.5**. Continuous
  mode is the baseline for maintaining visibility. The vanilla profile has precipitation flags rather than an intensity parameter.
- **Open World:** optional read-only reflection reads its eased local precipitation in **0–1**, with its own 0.02 cutoff, altitude-based
  rain/snow split (including sleet), origin-height offset and prevailing storm wind. This is independent of weather particle render-budget cuts.
  No Open World install is needed for vanilla tracks. The adapter retries discovery if Open World loads later and warns once if its internal
  interface changes, then falls back to the active vanilla weather flags.
- Intended overlapping tuning bands are **light (0–0.33): intermittent**, **moderate (0.33–0.67): continuous**, and **heavy (0.67–1): fast**.
  Deposition varies continuously rather than jumping at those boundaries. Vehicle speed increases exposure, so conditions can favor a faster
  mode within a band. Final visual balance requires driving in the game.

`[Wipers] WeatherEffects = true` enables the new effects; set false and restart to retain the controls/blades alone.
Disabling `[Wipers] Enabled` or unloading the mod removes the precipitation overlay with the wipers. Body changes rebuild the surface and
start it clear; camera changes keep the accumulated state. Original glass meshes/materials and the world's weather particles are untouched.

Validation: compile against HalfGripRefs and run `tests/MirrorAdjustment.Tests.csproj`. The precipitation tests cover separate rain/slush
buildup and impact decay, wipe coverage and parked blades, intensity/vehicle exposure, reversed airflow and slower slush movement, washer
deposition, mode balance, frame rates, invalid inputs and field bounds. Existing cockpit-control tests remain included. These checks exercise
the model, not Unity rendering or the live optional-mod adapter.

**In-game playtest required:**

- On a vanilla rainy track, stop with wipers off and confirm impacts/buildup; compare intermittent, continuous and fast while stationary and driving.
- Try a snowy track: slush should persist after snowfall stops and clear only under the rubber, leaving the unswept edges.
- In Open World, drive between clear sky, drizzle and heavy rain, then cross the snow line. Confirm gradual intensity changes and mixed precipitation.
  Compare behavior at different graphics budgets and with Open World absent. Verify no adapter warning with the supported current version.
- Drive forward, reverse and across the prevailing wind; check impact density and streak directions. Use the washer on a dry track.
- Switch cameras, pause, change car bodies, restart the race and disable the mod. Check that the overlay follows only the front glass,
  stays behind cockpit controls, freezes while paused, rebuilds on body changes and disappears completely on unload.

With camera refraction disabled, the fallback is a procedural transparent deposit/highlight layer; that fallback does not refract the scene.
Visual scale, brightness, sorting, performance and live weather transitions still need the playtest above. No game runtime is available here.

## Windshield-wiper prototype

Version **0.5.3** on `test/windshield-wipers` includes the tested turn-signal work and adds a shorter stalk on the right of the steering column,
angled upward and stopping before the shifter. The grip uses the same code-drawn 128×20 pixel artwork and depth-cutout rendering as the signal stalk,
with a windshield symbol and a speed sleeve marked **0 / I / II / III**. No external images are needed.

- **Left-click and grab the end, then pull down:** spray and wipe continuously while held past the wash detent. Tiny movements on grabbing do not trigger spray.
- **Release:** the stalk springs to neutral and the spray stops. Each washer pull guarantees **at least three full out-and-back wipe cycles**, including
  cycles completed while held. A quick tap finishes all three automatically. A longer hold keeps wiping, then finishes the current cycle on release.
  The blades then park if the dial is off, or return to the selected speed. A partial stroke already underway when washing starts does not count as a full cycle.
- **Hold right-click on the end, then drag up/down:** rotate the speed sleeve up through **off → slow → medium → fast**, or back down to off.
  Clicking without moving leaves the speed unchanged. The dial clicks between **exactly four fixed positions**, with a quiet mechanical click per change.
  The sleeve and markings stay on a detent even while held; there are no intermediate positions. Off and fast are hard stops.
  The native hand stays on the grip and the cursor returns there on release. The three running positions are:
  **slow/intermittent** (one normal 1.4-second wipe, then a 2.5-second parked pause), **medium/continuous** (1.4-second wipes without a pause),
  and **fast** (0.8-second wipes without a pause). Intermittent changes wipe frequency, while fast increases blade velocity.
- Washing interrupts any intermittent pause immediately. Its minimum wipe burst uses continuous normal speed, retaining fast if already selected.
  Afterward, intermittent mode resumes with a parked pause. Changing speed preserves blade position.
  Switching off finishes the washer minimum (if pending) and the current cycle before parking.

Two dark rubber/metal wipers are fitted to the actual front windshield triangles, with a tandem sweep constrained to the opening.
The geometry follows the glass's slight center bend and moves with the car. It rebuilds after a body change and remains visible in the outside view.
The driver washer nozzle is offset and aimed inward, separately from the blade pivot; the passenger spray retains its original trajectory.
In 0.6.0 the washer spray also deposits fluid on the glass and the blades clear accumulated water/slush as described above.
Fluid consumption and automatic rain sensing are not implemented.
The controls and selected speed start off each race. Pausing freezes the blades; losing focus or leaving the cockpit releases a held washer control.

CFG section `[Wipers]` provides `Enabled` (default true), `IntermittentPause` (2.5 seconds), `StalkLength` (3.5 cockpit units) and `StalkUpAngle` (18 degrees).
Restart after changing timing or placement. Existing mirror, windows and turn-signal settings remain available.

**In-game validation still required:** check reach and shifter clearance, all four dial positions, sustained wash and release at each speed,
off commands during both halves of a stroke, blade coverage and parking, then camera/body changes, pause, focus loss and mod disable.

## Turn-signal prototype

Version **0.4.6** on `test/turn-signals` starts from the merged `main` branch. It adds a stalk extending from the left side of the
steering column, two inset green indicator lamps on the dashboard immediately left of the gear display, synchronized amber overlays at the
stock front/rear lamp locations, and a relay tick/tock sound.

The stalk is hand-drawn directly in code on a **128×20 pixel grid**, using seven opaque graphite/steel colors plus transparency.
Stepped shading, a small beveled grip, stamped arrows, metal edge highlights and chunky rubber ribs match the cockpit's low-resolution style.
The grip and boot retain their proportions as the shaft stretches to the configured length. Its placement and native grab point are preserved.
No external image file is required. Future cockpit artwork should follow this code-drawn, limited-palette approach.
The stalk uses an alpha-cutout material with depth writes: the steering wheel covers it, while the instrument cluster and moving needles stay behind it.

The stalk extends beyond the wheel rim and rests at an upward angle, clear of the radio. Its enlarged grip hitbox takes priority over
the wheel where they overlap. It moves 14 degrees to each detent; input follows the actual lever arc so both directions have equal sensitivity.

Grab the stalk's end with the normal hand control and move it up/down:

- **Down = left; up = right.** Halfway starts the signal without latching.
- A quick halfway tap returns the stalk to neutral on release and completes three flashes total. Holding halfway keeps blinking
  beyond that count; release then ends the signal. Releasing early finishes the remaining flashes.
- Full travel latches the signal and keeps the stalk at that end after release. Pull it back to neutral to cancel manually.
- Automatic cancellation requires steering into the indicated turn, actual forward travel and heading change, then steering returning
  near center. Turning the wheel while parked does not cancel. Holding the stalk manually prevents automatic return until release.

Cockpit indicators use smaller pixel-style arrow outlines in dark beveled lenses. Both remain dimly visible when off, with a restrained
green glow on the active lamp. The pair is compacted to fit the marked panel between the wheel and gear display, anchored to that display's
frame rather than the wheel rim. They blink with the exterior overlays, which are attached alongside the existing lamp meshes. The original headlights,
brake-light materials, steering, and car physics are not modified. The stalk stays fixed to the column rather than spinning with the wheel.
Signals continue in the outside view while the interior stalk/arrows are hidden. Changing bodies rebuilds the exterior overlays.
Disabling Slap Pack removes the stalk, arrows, overlays, and audio. Signals are local only: they are not replicated to remote
multiplayer cars, and hazards are not implemented.

A short mechanical **tick** sounds when the lamp turns on and a lower **tock** when it turns off. Audio is synthesized locally and routed
through the game's cabin/SFX mixer. It plays in the cockpit only, stops when paused or disabled, and does not replay missed clicks on return.
Sound generation failure logs a warning without disabling the visual signals.

Settings in `BepInEx/config/halfgrip.slappack.cfg` (restart after changing timing/thresholds):

```ini
[TurnSignals]
Enabled = true
ShortFlashes = 3
BlinkHalfPeriod = 0.4
CancelArmSteering = 12
CancelCenteredSteering = 4
CancelMinimumTurn = 15
StalkExtraLength = 1.8
StalkUpAngle = 30
ClickSound = true
ClickVolume = 0.35
```

`BlinkHalfPeriod` is seconds for each on/off phase (0.4 gives 75 flashes/minute). Steering and heading thresholds are degrees.
`StalkExtraLength` is cockpit units beyond the measured wheel radius; `StalkUpAngle` is the neutral upward angle in degrees.
Restart after changing stalk placement.
`ClickVolume` ranges from 0 to 1 and also respects the game's mixer volume. Set `ClickSound = false` to mute the relay.
Cancellation requires forward speed above 1 game unit/second. Custom bodies without recognized lamp meshes retain the cockpit indicators
and log a warning about missing exterior locations.

### Turn-signal playtest

- Confirm the stalk emerges from the left of the column, stays clear of the wheel rim, and can be grabbed without steering the wheel.
- Check both halfway tap directions give three flashes and return to neutral. Hold each halfway for several seconds, then release.
- Check full down/up latches until manually centered, and that all cockpit/exterior lamps blink on the correct side in sync.
- Drive left and right turns, recenter steering, and confirm the stalk and lamps cancel. Repeat wheel movement while parked and confirm
  it stays latched. If cancellation feels too early/late, report the turn and configured thresholds.
- Check the compact lamp pair fits the marked dashboard space just left of the gear display, clear of the wheel rim and gauges.
- Check both off-state lenses remain visible and the active arrow is readable without the old oversized solid green shape.
- Listen for one tick/tock pair per blink, including short taps, held-half signals and cancellation. Check pause, outside view, mute and
  volume settings, and confirm the other car audio is unaffected. Report whether the default relay volume is too loud or too quiet.
- Switch camera views and body models, pause/lose focus while holding the stalk, and disable Slap Pack to check cleanup.
- Check mirror, window cranks, shifting, braking, headlights, and their hand indicators still behave normally.

## Window-crank development branch

`test/window-cranks` starts from the working mirror branch. Version **0.3.2** anchors the crank direction symbols over the held hand on
both doors. The window prototype and rotation correction have been reported working in-game; the symbol-placement fix still needs a playtest.

Look toward either door, grab its existing crank artwork with the normal hand control, and move the hand in circles. Clockwise lowers
that window; counterclockwise raises it. The two windows move independently, stop at the ends, and start fully raised each race.
Release leaves the window at its current height. Three crank turns span the default full travel.

The cranks are painted into the door atlas. Slap Pack locates the artwork through the original UVs, replaces its static image in a
private material/texture copy, and rotates an isolated copy of that same artwork at its original location. No game artwork is shipped
with the mod. Unsupported cabin textures are skipped with a warning instead of modifying unrecognized doors.

Only the two matching front door glass regions receive a faint blue-gray **4% opacity** effect. Lowered glass is clipped to its original
opening so it disappears into the door rather than protruding through the frame. The windshield, rear glass, source meshes, and shared
glass material are unchanged. This prototype changes visible glass only; it does not change glass damage, bullet collision, rain, or audio.

Settings in `BepInEx/config/halfgrip.slappack.cfg` (restart after editing tint or travel):

```ini
[Windows]
Enabled = true
TintOpacity = 0.04
TurnsForFullTravel = 3
WriteGeometryReport = false
```

Tint opacity allows 0–0.15; travel allows 1–8 turns. Disabling windows or unloading Slap Pack removes the added panes/controls and restores
the original door materials. Camera switching retains travel; changing car models rebuilds the window bindings.

## Optional window geometry diagnostics

The supplied runtime GPU report identified the stock crank UVs and front door panes for both stock cabin meshes. To collect another
report for troubleshooting, set `WriteGeometryReport = true`, start a race, and look for `Window-crank geometry report written to`
in `BepInEx/LogOutput.log`. The report folder is:

```text
<game>/BepInEx/SlapPack/WindowDiagnostics/<scene>-<timestamp>/
  geometry.txt
  texture-*.png
```

Share that folder for inspection; it contains the complete cabin/car hierarchy, mesh vertices/UVs/triangles, and material
texture previews at up to 1024 pixels. Version 0.2.1 also tries a synchronous GPU-buffer read for meshes whose CPU copy was discarded;
unsupported formats or readback failures are recorded in `geometry.txt`. Shared mesh data and usage flags are never modified.
These are private game reference assets:
keep the reports out of the mod repository and distribution archives. The diagnostic does not change shared meshes, materials, or glass.
Try each car model you want supported, since their doors can differ.

The diagnostic runs once per spawned player car. Turn it off after troubleshooting; it can pause the game briefly while reading GPU data.

The first prototype implements the adjustable **interior rear-view mirror** for Half Grip **0.418**, using **HalfGripTools 1.0.6+**.
It compiles against the game's reference assemblies; in-game behavior is still awaiting validation.

## Install and try the mirror

Copy the `SlapPack` folder into `<game>/BepInEx/HalfGripMods/` alongside your other HalfGripTools mods. The layout must be
`HalfGripMods/SlapPack/mod.json` and `HalfGripMods/SlapPack/src/*.cs`.
HalfGripTools compiles the source when the game starts.
The development `.csproj` and its `bin`/`obj` folders are not needed in the game. Restart the game after changing source.

- In the cockpit, grab the mirror casing with the normal hand/grab control and drag. The casing and glass tilt together
  around the top mount, moving the mirror's center and adjusting the rearward view. Its grab hitbox follows the visible mirror.
- Release to retain and save the adjustment, leaving the hand cursor at the mirror's grip point. Right-click while hovering over
  or holding the mirror to reset both axes.
- Defaults allow 30 degrees horizontally and 20 degrees vertically. Sensitivity and limits are configurable in
  `BepInEx/config/halfgrip.slappack.cfg`; `SavedYaw` and `SavedPitch` store the shared setting across races.
- Pausing, losing focus, releasing the hand, or switching out of the cockpit cancels an active drag.
- The mirror stays behind overlapping hand cursor sprites, including hover, hold, and release transitions, as its angle changes.
- `MirrorFOV = 22` sets the rear-view camera's vertical field of view in degrees (range 10–60). Lower values make the rearward
  view larger and narrower. The original is about 28 degrees; unloading restores the FOV captured before Slap Pack changed it.
- `IncludeCarInterior = true` (default) in the CFG's `[Mirror]` section includes the layers used by the car's 3D cabin models in
  the rear-view camera. Set it to `false` to restore the original layer inclusion. This changes the rear-view camera only; it does
  not add the separate 2D cockpit/hand layer. Other geometry sharing the cabin's layer can also appear in the reflection.
- If the Smart Mirror upgrade is active, manual adjustment suspends its auto-aim. Auto-aim resumes when Slap Pack unloads.

Example in `BepInEx/config/halfgrip.slappack.cfg` (edit while the game is closed, then restart):

```ini
[Mirror]
MirrorFOV = 22
IncludeCarInterior = true
```

The mirror surface tilts by half the camera adjustment angle, approximating how reflection doubles the change in viewing direction.
Right-click reset restores both its original pose and view. Unloading also restores the original hierarchy and removes the added hitbox.
Additional visual hints and the other planned controls are not implemented yet. HalfGripTools does not load gameplay mods in speedrun mode.

## Build and check

Use the .NET 8 SDK, a sibling `HalfGripRefs` checkout, and a built HalfGripTools DLL. The private game DLLs remain in HalfGripRefs;
do not commit or distribute them with this mod.

```bash
# In the prepared cloud environment, activate dotnet first:
source /workspace/.halfgrip-setup/activate.sh

# From the HalfGripSlapPack repository:
dotnet build SlapPack/SlapPack.csproj -c Release \
  -p:HalfGripToolsDll=/workspace/.halfgrip-setup/build/tools/HalfGripTools.dll
dotnet run --project tests/MirrorAdjustment.Tests.csproj -c Release
```

For another layout, pass `-p:HalfGripRefsDir=/absolute/path/to/HalfGripRefs` and your own `HalfGripToolsDll` path.
The 130 tests exercise two-axis sensitivity, both stops, reversal at a stop, restored values, reset, invalid numeric inputs,
hand depth clearance as the mirror approaches or recedes, and camera-layer inclusion/restoration without losing unrelated changes.
The window tests cover proportional multi-turn travel, raising/lowering, stops without windup, independent doors, angular seam crossing,
and invalid input. Pane tests cover fully raised/lowered states, partial travel, clipping within sloped frames, and duplicate triangle vertices.
Additional tests check half-precision GPU vertex decoding, including subnormal and invalid values. These tests do not exercise Unity's
GPU readback API; that path requires an in-game diagnostic run.
Turn-signal tests cover direction mapping, exact short-flash completion, held-half behavior, full detents, manual cancellation, side
changes, blink timing, left/right turn completion, parked/opposite-turn rejection, held cancellation override, and invalid inputs/settings.
Relay tests cover click edges, cancellation, side changes, silent-phase handling, the paired three-flash sequence, sample duration,
finite unclipped output, transient decay, and deterministic distinct tick/tock waveforms. Mixer routing and perceived sound quality require in-game testing.
Stalk layout tests check grip alignment, undistorted grip/boot proportions across lengths, joined art sections, pivot alignment, and invalid lengths.
Wiper tests cover all speed detents, distinct speeds, held washing, release and self-parking, speed changes without snapping, wash override,
pause and invalid input, plus blade clearance through the entire sweep on trapezoidal and stock-shaped glass.
Knob tests cover stationary clicks, up/down dragging, steady holds, detent hysteresis, hard stops without windup, release/re-grab behavior,
invalid input, phase continuity and exactly four discrete positions throughout a drag. Washer tests cover short taps, long holds, partial strokes,
repeated pulls, off commands, and speed restoration after the three-cycle minimum. Spray tests check the inward driver adjustment and unchanged passenger trajectory.
Intermittent tests verify normal-speed strokes followed by motionless parked pauses, continuous modes without delays, mode changes during a pause,
washer overrides and return to intermittent operation, and matching results across large versus small frame steps.
Pixel-art checks enforce the native 128×20 resolution, small opaque palette, transparent border, and connected grip/shaft/boot silhouette.

## Window prototype playtest

- Confirm the log reports `Windows bound` and that both cranks appear in their original locations without duplicate artwork or a bright square patch.
- Look toward each door and try several clockwise/counterclockwise turns. Check that the visible crank and hand stay aligned, each window moves
  independently, and reversing at the stops responds immediately.
- Confirm clockwise and counterclockwise hand movement rotates the visible crank in the same direction on both doors.
- While cranking either door, confirm the direction symbols stay over the hand as the view turns, instead of appearing on the dashboard.
  Release and try steering/shifting to confirm their normal indicators still work.
- Check tint at fully raised, half raised, and fully lowered states. The upper edge should descend into the door; windshield and rear glass should
  remain unchanged. Report whether the default 4% opacity is visible enough.
- Release/re-grab, pause, lose focus, switch to the outside camera and back, restart a race, and try the other stock car model.
- Disable Slap Pack and confirm original door artwork/materials return and all added tint and controls disappear.
- Check existing mirror, steering, gun, and nearby cockpit controls still work.
They do not validate Unity input, scene binding, or rendering.

## In-game acceptance checks

- Start a race and confirm the log reports `Adjustable mirror attached` once for the cockpit.
- Drag each axis independently. Confirm the view moves in the expected direction and the hand targets the mirror only.
- Confirm the casing and glass move together around the fixed top mount, with no separation or clipping. Check that the hitbox and
  held hand follow the mirror at all four adjustment extremes, and that repeated drags do not drift when the housing tilts.
- At each extreme, hover, grab, release, and pass the idle hand across the glass. Confirm the screen stays behind every hand pose
  without flickering, and that cursor screen position and release behavior remain unchanged.
- Reach each stop and reverse direction; adjustment should respond immediately without a dead zone.
- Release, grab again, and right-click reset both while hovering and while holding. Check for jumps or drifting.
- Drag well beyond an adjustment stop, then release. Confirm the hand remains at its visible grip instead of jumping to the hidden
  input cursor, and that the next small mouse movement starts from that location. Repeat with a normal drag and after resetting while held.
- Pause or lose focus during a drag, release the button, then return. Confirm no stuck hand or continued adjustment.
- Switch to the outside camera and back; restart the race, change tracks, and restart the game. Confirm the setting survives.
- Try with and without the Smart Mirror upgrade. Disable mods through HalfGripTools and confirm the original view and auto-aim return.
- Check normal steering, shifting, and other nearby cockpit interactions remain usable.
- Compare `MirrorFOV = 22` with `28` and confirm only the reflection changes. Confirm disabling Slap Pack restores the original FOV.
- With `IncludeCarInterior = true`, angle the mirror toward the cabin and check the 3D interior is visible for each car model.
  Repeat with `false` to verify the original layer inclusion. Check the forward camera, hand, and mirror screen remain unchanged,
  and confirm disabling Slap Pack restores the rear-view camera's original layer inclusion.

Compile checks and the adjustment tests pass in the cloud environment. The camera-only prototype has been reported working in-game;
the moving housing/glass update still needs an in-game playtest. The game runtime is not available here, so visual alignment, clipping,
hand tracking, and reset/unload behavior must be checked in that playtest.
