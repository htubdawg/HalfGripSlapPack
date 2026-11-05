# SlapPack development conventions

- Develop changes on focused branches and merge only when the user requests it. The user authorized bringing the completed weather/wiper/signal work into `main` for 0.8.3; leave `test/windshield-wipers` unchanged.
- Use tabs for new C# indentation and `///` summaries for new types and nontrivial methods. Avoid unrelated formatting changes.
- Use point filtering for visible textures/copies; retain the game's hard-pixel appearance.
- Gate routine information logs behind `[Diagnostics] VerboseLogging`. Keep warnings/errors visible. Explicitly requested geometry diagnostics may report their output path.
- Use the existing cockpit world camera. Scene-color sampling must stay in the same frame and declare RenderGraph texture/depth dependencies.
- Do not add a second scene-render camera or depend on a captured previous frame. Never change shared game materials/meshes or original camera settings.
- Private game DLLs and reference assets remain outside this repository and any distribution. Build against HalfGripRefs and HalfGripTools; see README.
- Run the compile check and existing model tests for gameplay/rendering changes. Do not claim that these validate Unity/GPU rendering or live Open World behavior.
- Update STATUS.md with the final behavior, validation and remaining runtime checks.

- Increment `SlapPack/mod.json` for every player-facing behavior update, including test branches. Keep the README version and development project Version aligned so installed test builds are identifiable.
- Preserve author/credit metadata exactly unless the user explicitly supplies a replacement. Never infer display credit from a repository owner, Git identity or tool account.
- Slap Pack author credit is `Codex - Oretsej`: Codex is the assistant used for this mod; Oretsej is the creator credit. Preserve this exact text. Do not copy another assistant's name from other mods.
- Keep the installable `SlapPack/` folder limited to `mod.json` and runtime `src/`; development projects and generated build outputs belong outside it. The compile-check project is repository-root `SlapPack.csproj`.
