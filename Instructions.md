# Instructions

## Overview

This document is the practical usage guide for Kitsub. All commands and options shown
here are derived from the CLI command definitions in the repository.

## Install / Build

Build from source:

```powershell
dotnet build Kitsub.sln

dotnet publish src/Kitsub.Cli -c Release -r win-x64 --self-contained false
```

## CLI overview

Discover commands and options:

```powershell
kitsub --help
kitsub <command> --help
```

Top-level commands:

- `inspect`
- `mux`
- `burn`
- `fonts` (`attach`, `check`)
- `extract` (`audio`, `sub`, `video`)
- `convert` (`sub`)
- `tools` (`status`, `fetch`, `clean`)
- `release` (`mux`)
- `config` (`path`, `init`, `show`)
- `doctor`

## Examples

### Inspect a media file

**Goal:** Print stream/track information for a media file.

**Inputs:**
- `INPUT_FILE` — path to a media file (placeholder)

**Command:**

```powershell
kitsub inspect "INPUT_FILE"
```

**Expected result:** Track information is printed to the console.

### Generate a MediaInfo JSON report

**Goal:** Save a MediaInfo JSON report for a file.

**Inputs:**
- `INPUT_FILE` — path to a media file (placeholder)

**Command:**

```powershell
kitsub inspect mediainfo "INPUT_FILE"
```

**Expected result:** A JSON report is saved under `./reports/mediainfo/<YYYYMMDD>/` with a generated filename.

### Mux subtitles into an MKV

**Goal:** Add subtitle tracks to an MKV file.

**Inputs:**
- `INPUT_MKV` — input MKV file (placeholder)
- `SUB_FILE` — subtitle file (placeholder)
- `OUTPUT_MKV` — output MKV file (placeholder)

**Command:**

```powershell
kitsub mux --in "INPUT_MKV" --sub "SUB_FILE" --lang eng --title "English" --default --out "OUTPUT_MKV"
```

**Expected result:** A new MKV is written to the output path.

### Burn subtitles into a video

**Goal:** Render a subtitle file directly into video output.

**Inputs:**
- `INPUT_FILE` — input media file (placeholder)
- `SUB_FILE` — subtitle file (placeholder)
- `OUTPUT_FILE` — output video file (placeholder)

**Command:**

```powershell
kitsub burn --in "INPUT_FILE" --sub "SUB_FILE" --out "OUTPUT_FILE" --crf 18 --preset medium
```

**Expected result:** A new video file is written to the output path.

### Release mux using a JSON spec

**Goal:** Mux multiple subtitle tracks using a spec file.

**Inputs:**
- `release.json` — release spec file (example below)

**Spec example:**

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

**Command:**

```powershell
kitsub release mux --spec ".\\release.json"
```

**Expected result:** An MKV file is generated according to the spec.

## Common patterns

### Global flags for tool-driven commands

These options are available on commands that run external tools:

- Tool paths: `--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`, `--mediainfo`
- Tool resolution: `--prefer-bundled <BOOL>`, `--prefer-path <BOOL>`, `--tools-cache-dir <PATH>`
- Provisioning: `--assume-yes`, `--no-provision`, `--no-startup-prompt`, `--check-updates`
- Logging/output: `--dry-run`, `--verbose`, `--log-file <PATH>`, `--log-level <trace|debug|info|warn|error>`, `--no-log`, `--no-banner`, `--no-color`, `--progress <auto|on|off>`

## Troubleshooting

- Logs default to `logs/kitsub.log` relative to the working directory unless overridden.
- Tool provisioning is only available on Windows; on other platforms, configure tool paths or install tools on PATH.
- Use `kitsub tools status` to check resolved tool paths.
