<!--
PHASE 1 NOTES
- Architecture: CLI uses Spectre.Console.Cli with CommandBase/ConfigCommandBase; tool resolution flows through ToolResolver + ToolBundleManager + ToolSettings/ToolingFactory; logging via Serilog; provisioning progress uses SpectreProgressReporter.
- Hook points: add a startup interceptor in Program.cs before command execution; add command-level gating in CommandBase via a virtual tool requirement method.
- Reused flags: existing tool path overrides, --dry-run/--verbose/--tools-cache-dir/--prefer-bundled/--prefer-path, plus new global --no-provision/--no-startup-prompt/--check-updates.
- Constraints: tool provisioning is Windows RID-only; update detection must compare cached toolset version to manifest toolsetVersion; skip updates when version cannot be resolved; respect non-interactive/CI and help-only invocations.
-->
