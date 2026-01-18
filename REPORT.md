# Kitsub Testability Report

## Phase 1 — Repository analysis

### Solution structure
- **Kitsub.Core** (`src/Kitsub.Core`, net10.0): small library defining media metadata models and selection helpers (e.g., `TrackSelection`).
- **Kitsub.Tooling** (`src/Kitsub.Tooling`, net10.0): tool orchestration layer for external tools (ffmpeg/ffprobe/mkvtoolnix/mediainfo), provisioning, and command building.
- **Kitsub.Cli** (`src/Kitsub.Cli`, net10.0): CLI application with Spectre.Console commands, config loading/validation, and logging configuration.
- **Kitsub.Tests** (`tests/Kitsub.Tests`, net10.0): existing xUnit project covering core + tooling logic, plus gated integration smoke tests.

### External dependencies (areas to isolate)
- **Filesystem & paths**: `File`, `Directory`, `Path`, `AppContext.BaseDirectory`, `Environment.SpecialFolder` (config loading, cache management, CLI validation).
- **Process execution**: `Process.Start`/`WaitForExitAsync` (external tool runner, CLI doctor checks).
- **Network**: `HttpClient` and archive downloads (tool provisioning in `ToolBundleManager`).
- **Environment**: environment variables for config and tool paths.
- **Time/temp**: temp file creation via `Path.GetTempPath()` and GUIDs.

### Existing tests
- `tests/Kitsub.Tests` already contains unit tests for core/formatting/tooling plus opt-in integration tests.

### Priority targets for unit tests (pre-test plan)
1. `LogLevelParser` (CLI log level parsing/validation)
2. `ToolSettingsApplier` (applying config defaults/overrides)
3. `ValidationHelpers` (file/dir validation result handling)
4. `ConfigWriter` (atomic config writes/backups)
5. `AppConfigDefaults` (baseline config values)
6. `TrackSelection` (core track selection logic)
7. `ToolResolver` (tool path resolution decisions)
8. `ExternalToolRunner.RenderCommandLine` (argument quoting)
9. `ToolsManifestLoader` (manifest parsing)
10. `FfmpegClient`/`FfprobeClient`/`MkvmergeClient` command builders

### Risks & testability issues
- **Provisioning & downloads** (`ToolBundleManager`) require network, archive extraction, and filesystem writes; best covered via interfaces like `IHttpClient`, `IFileSystem`, and mocked archive handlers.
- **Process execution** (`ExternalToolRunner`, CLI doctor checks) should be abstracted via an `IProcessRunner` for deterministic tests.
- **Environment & special folders** in `AppConfigLoader` complicate isolation; a small abstraction for environment paths would help.

## Phase 6 — Test coverage updates

### Added unit tests
- New xUnit test project for CLI logic with deterministic tests covering:
  - log level parsing (`LogLevelParser`)
  - config defaults (`AppConfigDefaults`)
  - settings application (`ToolSettingsApplier`)
  - validation helpers (`ValidationHelpers`)
  - atomic config writing (`ConfigWriter`)

### Not covered yet (and why)
1. `ToolBundleManager` provisioning flows (network + archive extraction).
2. `ExternalToolRunner` process execution (requires subprocesses).
3. Command handlers in `Kitsub.Cli` (higher-level integration with console + tooling).
4. `AppConfigLoader` full effective config loading (depends on real app data folders and env). 
5. Tool cache directory probing (requires filesystem permissions behavior).

### Recommendations
- Introduce small abstractions for filesystem and environment access (e.g., `IFileSystem`, `IEnvironment`), plus an `IProcessRunner` for process execution, to expand test coverage while keeping tests deterministic.
