# Kitsub

Kitsub is a cross-platform toolkit for video, audio, and subtitle workflows. The solution is split into a core library, tooling wrappers for external media tools, and a thin CLI layer based on Spectre.Console.

## Projects

- `src/Kitsub.Core` – models and shared utilities.
- `src/Kitsub.Tooling` – process runner, ffmpeg/ffprobe/mkvmerge/mkvpropedit clients, and operations.
- `src/Kitsub.Cli` – Spectre.Console CLI commands.

## Prerequisites

On Windows, Kitsub can provision `ffmpeg/ffprobe` and `mkvmerge/mkvpropedit` automatically. If you are running from source without packaged tools, ensure the external tools are in `PATH` or pass custom paths via options.

- `ffmpeg`, `ffprobe`
- `mkvmerge`, `mkvpropedit`

## Bundled Tools (Windows)

Kitsub ships a pinned manifest at `src/Kitsub.Tooling/Tools/ToolsManifest.json` that describes Windows tool archives and SHA256 sources. Tool resolution follows this order:

1. User overrides (`--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit`)
2. Portable bundled tools next to the app (`tools/win-x64/...`)
3. Cached provisioned tools in `%LOCALAPPDATA%/Kitsub/tools/win-x64/<toolsetVersion>/`
4. PATH fallback

To stage portable tools beside the CLI for publishing, run:

```powershell
./scripts/fetch-tools.ps1
```

The CLI also exposes tooling commands:

- `kitsub tools status` – show resolved tool paths and sources
- `kitsub tools fetch` – download and cache tool binaries
- `kitsub tools clean --yes` – delete the cached toolset

To publish a Windows build:

```powershell
./scripts/publish-win.ps1
```

## Build

```bash
# Build the solution
 dotnet build Kitsub.sln
```

## Run

```bash
# Run the CLI project
 dotnet run --project src/Kitsub.Cli -- --help
```

## Common Options

- `--dry-run` – print the exact command(s) without executing.
- `--verbose` – print commands and tool output.
- `--ffmpeg`, `--ffprobe`, `--mkvmerge`, `--mkvpropedit` – override tool paths.
- `--prefer-bundled` – prefer bundled tools (`true` by default).
- `--prefer-path` – prefer PATH-based tools (`false` by default).
- `--tools-cache-dir` – override the cache directory used for extracted tools.
- `--log-file` – override the log file path (default: `logs/kitsub.log`).
- `--log-level` – set the log level (`trace|debug|info|warn|error`).
- `--no-log` – disable file logging and log to the console only.

## Examples

Inspect media:

```bash
 dotnet run --project src/Kitsub.Cli -- inspect ./movie.mkv
```

Mux subtitles into MKV:

```bash
 dotnet run --project src/Kitsub.Cli -- mux --in ./movie.mkv --sub ./subs.en.srt --lang eng --title "English" --default --out ./movie.kitsub.mkv
```

Attach fonts:

```bash
 dotnet run --project src/Kitsub.Cli -- fonts attach --in ./movie.mkv --dir ./fonts --out ./movie.fonts.mkv
```

Check fonts:

```bash
 dotnet run --project src/Kitsub.Cli -- fonts check --in ./movie.mkv
```

Burn subtitles:

```bash
 dotnet run --project src/Kitsub.Cli -- burn --in ./movie.mkv --sub ./subs.ass --out ./movie.burned.mp4 --crf 18 --preset medium
```

Extract audio:

```bash
 dotnet run --project src/Kitsub.Cli -- extract audio --in ./movie.mkv --track 0 --out ./movie.audio.mka
```

Extract subtitles:

```bash
 dotnet run --project src/Kitsub.Cli -- extract sub --in ./movie.mkv --track eng --out ./movie.subs.srt
```

Extract video:

```bash
 dotnet run --project src/Kitsub.Cli -- extract video --in ./movie.mkv --out ./movie.video.mkv
```

Convert subtitles:

```bash
 dotnet run --project src/Kitsub.Cli -- convert sub --in ./subs.srt --out ./subs.ass
```
