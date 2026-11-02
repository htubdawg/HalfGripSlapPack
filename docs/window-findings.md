# Window-crank reference findings

The first runtime report was captured in `SCENE_TRACK_1_RaceTrack` using Slap Pack 0.2.0.

- `uv interior (fara bord)` includes recognizable crank artwork on the door-panel region. No object in the report is independently
  named as a window crank. Determine whether those faces are separable before replacing or duplicating the artwork.
- The 1024-pixel texture preview places the visible crank pivot approximately at image coordinates (104, 418), with its arm extending
  upward. This is a texture landmark for UV inspection, not an established mesh-space pivot or a universal binding for other skins.
- `CarInterior/Reference` is inactive and provides the cockpit-space reference geometry. The active 3D cabin is under the player car's
  `CameraSwitch.interiorModels` (`InteriorCar1` and `InteriorCar2`). Their meshes and transforms differ, so bindings must account for
  the selected model and the separate cockpit and world cameras.
- The cabin uses generic meshes such as `Cube.007`, `Cube.009`, and `Cube.010`. Their vertex counts/bounds are available, but all imported
  cabin meshes in the report have `readable=False` and no vertex/UV data.
- The `WindShield` mesh has 44 vertices and uses `GMTK/Glass Shatter`; the second body uses a separate `Cube.001` glass mesh. The exact
  door-pane faces need inspection before adding tint or movement. Do not tint the complete shared glass material or move the windshield.

Version 0.2.1 extends the diagnostic to read the existing vertex/index buffers into local arrays when the CPU copy is unavailable.
It handles Float32/Float16 positions/UVs and UInt16/UInt32 indices, accounts for submesh offsets/base vertices, and caches each mesh
snapshot for the duration of an export. It does not alter meshes, materials, or buffer usage flags. Runtime readback is still unverified.

Next: inspect the GPU-backed vertex/UV report to locate both crank faces/pivots and the door-glass outlines. Reuse the existing crank
geometry/artwork where feasible, bind each door to independent `WindowTravel`, and create the minimal visible glass effect on the side
panes only. If a device cannot export the buffers, diagnose the recorded error before choosing another supported readback route.

Do not commit the report, mesh data, or texture previews: they are private game reference assets.
