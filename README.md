# HalfGripSlapPack
A collection of new things to interact with in your car.

Features in development: adjustable rear-view mirror, working window cranks, turn signals, and windshield wipers.

## Turn-signal prototype

Version **0.4.4** on `test/turn-signals` starts from the merged `main` branch. It adds a stalk extending from the left side of the
steering column, two inset green indicator lamps on the dashboard immediately left of the gear display, synchronized amber overlays at the
stock front/rear lamp locations, and a relay tick/tock sound. **The new stalk artwork still needs an in-game playtest.**

The stalk now uses original shaded pixel artwork: a ribbed rubber mounting boot, tapered metal shaft, chamfered plastic grip, molded
ribs, edge wear and etched turn arrows. The grip and boot retain their proportions as the shaft stretches to the configured length.
Its placement and native grab point are preserved. Install the complete `SlapPack` folder, including `sprites/turn-signal-stalk.png`.

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
`HalfGripMods/SlapPack/mod.json`, `HalfGripMods/SlapPack/src/*.cs`, and `HalfGripMods/SlapPack/sprites/turn-signal-stalk.png`.
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
The sixty-five tests exercise two-axis sensitivity, both stops, reversal at a stop, restored values, reset, invalid numeric inputs,
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
