# HalfGripSlapPack
A collection of new things to interact with in your car.

Planned features: adjustable rear-view mirror, turn signals, windshield wipers, and working window cranks.

## Window-crank development branch

`test/window-cranks` starts from the working mirror branch. This first window build adds a tested, independent travel model for
each door and a runtime geometry report. **The cranks are not yet connected to interaction, and no tint or window animation is applied yet.**

The target is to reuse the driver's and passenger's existing cranks, with circular hand movement, approximately three turns for full
travel, and windows initially fully raised. Both door panes will receive a very light glass tint so their upper edge is visible as they
lower. The windshield will remain unchanged. Tint strength and the exact travel will be tuned against the actual geometry.

The available reference dumps collapse generic interior meshes and do not identify separate crank objects. They also lack vertex/UV
data needed to split door glass from a combined windshield/window mesh. A live geometry report is needed to decide whether the cranks
are separate meshes or artwork baked into the door texture, and to bind the correct panes without guessing positions.

To collect it, install this build as below, start a race, and wait a few seconds. Look for `Window-crank geometry report written to`
in `BepInEx/LogOutput.log`. The report folder is:

```text
<game>/BepInEx/SlapPack/WindowDiagnostics/<scene>-<timestamp>/
  geometry.txt
  texture-*.png
```

Share that folder for inspection; it contains the complete cabin/car hierarchy, readable mesh vertices/UVs/triangles, and material
texture previews at up to 1024 pixels. Unreadable meshes are reported rather than modified. These are private game reference assets:
keep the reports out of the mod repository and distribution archives. The diagnostic does not change shared meshes, materials, or glass.
Try each car model you want supported, since their doors can differ.

The diagnostic runs once per spawned player car. To stop generating reports, set this in `BepInEx/config/halfgrip.slappack.cfg`:

```ini
[Windows]
WriteGeometryReport = false
```

The first prototype implements the adjustable **interior rear-view mirror** for Half Grip **0.418**, using **HalfGripTools 1.0.6+**.
It compiles against the game's reference assemblies; in-game behavior is still awaiting validation.

## Install and try the mirror

Copy the `SlapPack` folder into `<game>/BepInEx/HalfGripMods/` alongside your other HalfGripTools mods. The layout must be
`HalfGripMods/SlapPack/mod.json` and `HalfGripMods/SlapPack/src/*.cs`. HalfGripTools compiles the source when the game starts.
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
The twenty-three tests exercise two-axis sensitivity, both stops, reversal at a stop, restored values, reset, invalid numeric inputs,
hand depth clearance as the mirror approaches or recedes, and camera-layer inclusion/restoration without losing unrelated changes.
The window tests cover proportional multi-turn travel, raising/lowering, stops without windup, independent doors, angular seam crossing,
and invalid input. Runtime window binding and tint remain pending the geometry report.
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
