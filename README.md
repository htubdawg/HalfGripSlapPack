# HalfGripSlapPack
A collection of new things to interact with in your car.

Planned features: adjustable rear-view mirror, turn signals, windshield wipers, and working window cranks.

The first prototype implements the adjustable **interior rear-view mirror** for Half Grip **0.418**, using **HalfGripTools 1.0.6+**.
It compiles against the game's reference assemblies; in-game behavior is still awaiting validation.

## Install and try the mirror

Copy the `SlapPack` folder into `<game>/BepInEx/HalfGripMods/` alongside your other HalfGripTools mods. The layout must be
`HalfGripMods/SlapPack/mod.json` and `HalfGripMods/SlapPack/src/*.cs`. HalfGripTools compiles the source when the game starts.
The development `.csproj` and its `bin`/`obj` folders are not needed in the game. Restart the game after changing source.

- In the cockpit, grab the mirror casing with the normal hand/grab control and drag to adjust the rearward view.
- Release to retain and save the adjustment. Right-click while hovering over or holding the mirror to reset both axes.
- Defaults allow 30 degrees horizontally and 20 degrees vertically. Sensitivity and limits are configurable in
  `BepInEx/config/halfgrip.slappack.cfg`; `SavedYaw` and `SavedPitch` store the shared setting across races.
- Pausing, losing focus, releasing the hand, or switching out of the cockpit cancels an active drag.
- If the Smart Mirror upgrade is active, manual adjustment suspends its auto-aim. Auto-aim resumes when Slap Pack unloads.

This first version changes the camera view while keeping the existing casing and glass sprites fixed. Mirror housing animation,
additional visual hints, and the other planned controls are not implemented yet. HalfGripTools does not load gameplay mods in speedrun mode.

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
The eight tests exercise two-axis sensitivity, both stops, reversal at a stop, restored values, reset, and invalid numeric inputs.
They do not validate Unity input, scene binding, or rendering.

## In-game acceptance checks

- Start a race and confirm the log reports `Adjustable mirror attached` once for the cockpit.
- Drag each axis independently. Confirm the view moves in the expected direction and the hand targets the mirror only.
- Reach each stop and reverse direction; adjustment should respond immediately without a dead zone.
- Release, grab again, and right-click reset both while hovering and while holding. Check for jumps or drifting.
- Pause or lose focus during a drag, release the button, then return. Confirm no stuck hand or continued adjustment.
- Switch to the outside camera and back; restart the race, change tracks, and restart the game. Confirm the setting survives.
- Try with and without the Smart Mirror upgrade. Disable mods through HalfGripTools and confirm the original view and auto-aim return.
- Check normal steering, shifting, and other nearby cockpit interactions remain usable.

Compile checks and the adjustment tests pass in the cloud environment. All in-game acceptance checks remain unrun because the game runtime
is not available here. Direction, hitbox size, and sensitivity should be tuned from that first playtest.
