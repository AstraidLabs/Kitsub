# Kitsub

Kitsub is a Windows-focused (.NET 10) CLI for video, audio, and subtitle workflows
commonly used in anime/fansub pipelines. It wraps FFmpeg/ffprobe and MKVToolNix
(mkvmerge/mkvpropedit) and can provision those tools automatically.

## Key Features

- Inspect media streams and metadata.
- Generate MediaInfo CLI reports.
- Mux subtitle tracks into MKV files.
- Check required fonts and attach them to MKV containers.
- Extract audio, video, and subtitle streams.
- Burn subtitles into video.
- Convert subtitles between common formats (basic).
- File logging with rolling logs via Serilog.
- Dry-run and verbose output for safe previews.
- Tool provisioning (bundled or cached) with status reporting.

## Requirements

- Windows x64.
- .NET 10 runtime (or a self-contained release build).
- Disk space for bundled tools and/or cached downloads.

## Installation

**Option A: Download release ZIP (portable)**

1. Download the Windows release ZIP.
2. Extract it anywhere.
3. Run `kitsub.exe` from that folder.

**Option B: Build from source**

```powershell
# Build
 dotnet build Kitsub.sln

# Publish a Windows build
 dotnet publish src/Kitsub.Cli -c Release -r win-x64 --self-contained false
```

**.NET global tool**

Planned. (If this becomes available it will be documented here.)

## Quick Start

```powershell
# Inspect a file
 kitsub inspect "[SubsPlease] Frieren - 01 (1080p).mkv"
```

```powershell
# Generate a MediaInfo report
 kitsub inspect mediainfo "[SubsPlease] Frieren - 01 (1080p).mkv"
```

```powershell
# Mux subtitles into an MKV
 kitsub mux --in "[SubsPlease] Frieren - 01 (1080p).mkv" \
   --sub "[SubsPlease] Frieren - 01.en.ass" --lang eng --title "English" \
   --default --out "Frieren - 01.with-subs.mkv"
```

```powershell
# Check fonts required by subtitle tracks
 kitsub fonts check --in "Frieren - 01.with-subs.mkv"
```

```powershell
# Attach fonts from a folder
 kitsub fonts attach --in "Frieren - 01.with-subs.mkv" --dir .\fonts \
   --out "Frieren - 01.fonts.mkv"
```

```powershell
# Extract streams
 kitsub extract audio --in "Frieren - 01.fonts.mkv" --track 0 --out "frieren-01.audio.mka"
 kitsub extract sub --in "Frieren - 01.fonts.mkv" --track eng --out "frieren-01.subs.ass"
 kitsub extract video --in "Frieren - 01.fonts.mkv" --out "frieren-01.video.mkv"
```

```powershell
# Burn subtitles into video
 kitsub burn --in "Frieren - 01.fonts.mkv" --sub "frieren-01.subs.ass" \
   --out "frieren-01.burned.mp4" --crf 18 --preset medium
```

```powershell
# Tools status and fetch
 kitsub tools status
 kitsub tools fetch
```

## Commands

- `inspect` — Inspect media streams and metadata.
- `inspect mediainfo` — Generate a MediaInfo CLI report.
- `mux` — Add subtitle tracks to an MKV.
- `burn` — Burn subtitles into video.
- `convert sub` — Convert subtitles between formats.
- `fonts check` — Report missing/required fonts.
- `fonts attach` — Attach fonts to an MKV.
- `extract audio` — Extract audio streams.
- `extract video` — Extract video streams.
- `extract sub` — Extract subtitle streams.
- `tools status` — Show resolved tool paths and sources.
- `tools fetch` — Download and cache tool binaries.
- `tools clean` — Delete cached tools.
- `config path` — Show resolved configuration paths.
- `config init` — Initialize the global configuration file.
- `config show` — Display the global or effective configuration.
- `doctor` — Run diagnostics and tool checks.

## Tool Provisioning (No Manual Installs)

Kitsub resolves tool binaries automatically. You can override any tool path when
needed, but most users never need to install FFmpeg, MKVToolNix, or MediaInfo
manually. MediaInfo CLI is downloaded from mediaarea.net the first time you run
`kitsub inspect mediainfo` or `kitsub tools fetch`.

**Modes**

- **Bundled:** tools are shipped under `./tools/win-x64/...` alongside the app.
- **Cache:** tools are downloaded and extracted to
  `%LOCALAPPDATA%\Kitsub\tools\...`.

**Resolution priority**

1. CLI overrides (`--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`, `--mediainfo`)
2. Config file overrides (global + per-tool)
3. Environment variable overrides
4. Bundled tools next to the app
5. Cached tools
6. PATH fallback

**Commands**

```powershell
 kitsub tools status
 kitsub tools fetch
 kitsub tools clean --yes
```

**Overrides**

- `--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`, `--mediainfo`
- `--tools-cache-dir`
- `--no-provision` (MediaInfo inspection only)
- Config: `tools.mediainfo`

**Integrity**

Provisioned archives are verified against pinned SHA256 hashes. If a secondary
`sha256Url` is configured and differs from the pinned hash, provisioning fails.

## Logging

Kitsub logs to rolling files via Serilog. By default, logs are written under
`./logs/` relative to the working directory (for example `logs/kitsub.log`).

**Flags**

- `--log-file` — override the log file path.
- `--log-level` — set the log level (`trace|debug|info|warn|error`).
- `--verbose` — print commands and tool output to the console.
- `--dry-run` — show commands without executing.

**Troubleshooting workflow**

```powershell
 kitsub inspect "Frieren - 01.mkv" --verbose --log-level debug
 # Share the log file with the issue report (redact sensitive paths if needed).
```

## Configuration

**Files (Windows)**

- Global config: `%APPDATA%\Kitsub\kitsub.json`
- Per-tool override files (optional):
  - `%APPDATA%\Kitsub\kitsub.ffmpeg.json`
  - `%APPDATA%\Kitsub\kitsub.ffprobe.json`
  - `%APPDATA%\Kitsub\kitsub.mkvmerge.json`
  - `%APPDATA%\Kitsub\kitsub.mkvpropedit.json`

**Precedence (lowest → highest)**

1. Built-in defaults
2. Global config
3. Per-tool override files
4. Environment variables
5. CLI flags

**Environment variables**

- `KITSUB_CONFIG` — override the global config path.
- `KITSUB_FFMPEG`, `KITSUB_FFPROBE`, `KITSUB_MKVMERGE`, `KITSUB_MKVPROPEDIT`, `KITSUB_MEDIAINFO`
- `KITSUB_TOOLS_CACHE_DIR`

**Commands**

```powershell
kitsub config path
kitsub config init
kitsub config init --force
kitsub config show
kitsub config show --effective
```

## Doctor

Run a full diagnostic sweep:

```powershell
kitsub doctor
```

The doctor command reports configuration health, tool resolution status, and
attempts `ffmpeg -version` / `mkvmerge -V` when available.

## Exit Codes

| Code | Meaning |
| ---- | ------- |
| 0 | Success |
| 1 | Validation/user input/config error |
| 2 | External tool failure |
| 3 | Provisioning/download failure |
| 4 | Provisioning integrity failure |
| 5 | Unexpected error |

Configuration is supported for tool paths, logging, UI toggles, and command
defaults. Use `kitsub config init` to create the baseline file.

## Roadmap

- AI-assisted subtitle translation (planned).
- Batch operations.
- Richer subtitle validation/linting.
- Presets for common fansub release layouts.

## Third-Party Tools & Licenses

Kitsub is MIT-licensed. It uses or bundles FFmpeg and MKVToolNix under their
respective licenses. See `THIRD_PARTY_NOTICES.md` and the `LICENSES/` directory
for details. This is informational only and not legal advice.

## Contributing

1. Fork the repo and create a feature branch.
2. Follow existing code style and keep changes focused.
3. Use clear commit messages.
4. Open a PR with context and testing notes.

## Security

Please report security issues privately. Do not include sensitive data in logs
or issue reports.
