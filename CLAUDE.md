# SlapPack development conventions

- Keep weather work on `test/windshield-weather`. Do not merge it into `test/windshield-wipers` or `main` without the user's instruction.
- Use tabs for new C# indentation and `///` summaries for new types and nontrivial methods. Avoid unrelated formatting changes.
- Use point filtering for visible textures/copies; retain the game's hard-pixel appearance.
- Gate routine information logs behind `[Diagnostics] VerboseLogging`. Keep warnings/errors visible. Explicitly requested geometry diagnostics may report their output path.
- Use the existing cockpit world camera. Scene-color sampling must stay in the same frame and declare RenderGraph texture/depth dependencies.
- Do not add a second scene-render camera or depend on a captured previous frame. Never change shared game materials/meshes or original camera settings.
- Private game DLLs and reference assets remain outside this repository and any distribution. Build against HalfGripRefs and HalfGripTools; see README.
- Run the compile check and existing model tests for gameplay/rendering changes. Do not claim that these validate Unity/GPU rendering or live Open World behavior.
- Update STATUS.md with the final behavior, validation and remaining runtime checks.
