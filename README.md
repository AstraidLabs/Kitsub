# Kitsub

Kitsub is a cross-platform toolkit for video, audio, and subtitle workflows. The solution is split into a core library, tooling wrappers for external media tools, and a thin CLI layer based on Spectre.Console.

## Projects

- `src/Kitsub.Core` – models and shared utilities.
- `src/Kitsub.Tooling` – process runner, ffmpeg/ffprobe/mkvmerge/mkvpropedit clients, and operations.
- `src/Kitsub.Cli` – Spectre.Console CLI commands.

## Prerequisites

Install the external tools and ensure they are in `PATH` or pass custom paths via options:

- `ffmpeg`, `ffprobe`
- `mkvmerge`, `mkvpropedit`

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
