# Tools Prompts Throttling Notes

## Current Behavior Summary
- Startup prompts are handled by `ToolsStartupCoordinator` through the Spectre CLI interceptor in `Program.cs`.
- Command-level tool gating and provisioning live in `CommandBase` (plus on-demand MediaInfo provisioning in `InspectCommand`).
- Tool resolution flows through `ToolResolver` + `ToolBundleManager` with cache/bundled resolution rules.

## Reused Flags
- `--no-provision`, `--no-startup-prompt`, `--check-updates` (existing CLI flags).
- `tools.autoUpdate`, `tools.updatePromptOnStartup`, `tools.checkIntervalHours`, `tools.commandPromptOnMissing` (config keys).

## Hook Points
- Startup: `Program.cs` interceptor → `ToolsStartupCoordinator.RunAsync`.
- Command gating: `CommandBase.EnsureRequiredToolsAsync` and `InspectCommand` MediaInfo path.

## Constraints
- Provisioning is Windows-only and gated by RID availability in the manifest.
- Update detection must compare cached toolset folder versions to the embedded `ToolsManifest` version.
- Update prompts are throttled by persisted state in `%LOCALAPPDATA%\Kitsub\state\startup.json`.
