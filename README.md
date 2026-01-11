# Kitsub

Kitsub is a Windows-focused (.NET 10) CLI for video, audio, and subtitle workflows
commonly used in anime/fansub pipelines. It wraps FFmpeg/ffprobe and MKVToolNix
(mkvmerge/mkvpropedit) and can provision those tools automatically.

## Key Features

- Inspect media streams and metadata.
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

## Tool Provisioning (No Manual Installs)

Kitsub resolves tool binaries automatically. You can override any tool path when
needed, but most users never need to install FFmpeg or MKVToolNix manually.

**Modes**

- **Bundled:** tools are shipped under `./tools/win-x64/...` alongside the app.
- **Cache:** tools are downloaded and extracted to
  `%LOCALAPPDATA%\Kitsub\tools\...`.

**Resolution priority**

1. CLI overrides (`--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`)
2. Bundled tools next to the app
3. Cached tools
4. PATH fallback

**Commands**

```powershell
 kitsub tools status
 kitsub tools fetch
 kitsub tools clean --yes
```

**Overrides**

- `--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`
- `--tools-cache-dir`

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

## Configuration (Optional)

Not yet. Planned: a user config file for default tool paths, log options, and
preset profiles.

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
