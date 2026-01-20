# Kitsub CLI Documentation

## Tool Provisioning Prompts

Kitsub can prompt to download bundled tools on startup and when a command requires missing tools. You can disable prompts with `--no-startup-prompt` or `--no-provision`, auto-accept provisioning with `--assume-yes` (not destructive confirmations like `tools clean --yes`), and force update checks (when `tools.autoUpdate` is true) with `--check-updates`. Configure defaults in `kitsub.json` under `tools.startupPrompt`, `tools.commandPromptOnMissing`, `tools.autoUpdate`, `tools.updatePromptOnStartup`, and `tools.checkIntervalHours`.

In non-interactive environments, Kitsub will not prompt; missing tool provisioning fails unless `--assume-yes` is provided. Startup update prompts are throttled to at most once per `checkIntervalHours`.

## Release Workflow (MKV Only)

The release workflow commands are MKV-only and will reject any non-MKV input or output.

### Single-sub Release Mux

```powershell
kitsub release mux --in "E01.mkv" --sub "CZ.ass" --lang ces --title "Czech" --default \
  --fonts ".\\fonts" --out ".\\Release\\E01.release.mkv"
```

### Multi-sub Release Mux (JSON spec)

Create a JSON spec file, for example `release.json`:

```json
{
  "input": "E01.mkv",
  "output": "E01.release.mkv",
  "fontsDir": ".\\fonts",
  "strict": false,
  "subtitles": [
    { "path": "CZ.ass", "lang": "ces", "title": "Czech", "default": true, "forced": false },
    { "path": "EN.ass", "lang": "eng", "title": "English", "default": false, "forced": false }
  ]
}
```

Run the command:

```powershell
kitsub release mux --spec ".\\release.json"
```

### Release Spec Schema

```json
{
  "input": "E01.mkv",
  "output": "E01.release.mkv",
  "fontsDir": ".\\fonts",
  "strict": false,
  "subtitles": [
    { "path": "CZ.ass", "lang": "ces", "title": "Czech", "default": true, "forced": false },
    { "path": "EN.ass", "lang": "eng", "title": "English", "default": false, "forced": false }
  ]
}
```

**Notes**

- Paths in the spec may be relative to the spec file directory.
- When `--spec` is provided, per-subtitle flags (`--sub`, `--lang`, `--title`, `--default`, `--forced`) are ignored.
- CLI overrides can replace spec values for `--in`, `--out`, `--out-dir`, `--fonts`, and `--strict`.
- Output resolution:
  - `--out` wins when provided.
  - Otherwise `--out-dir` + `<inputBaseName>.release.mkv`.
  - Otherwise `<inputDir>\<inputBaseName>.release.mkv`.
- `--default`/`--no-default` and `--forced`/`--no-forced` apply only to single-sub mode.
