# 🧰 Kitsub — subtitle-first CLI for video workflows

[![License](https://img.shields.io/github/license/AstraidLabs/Kitsub?label=license)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Type](https://img.shields.io/badge/Type-CLI-2F6F8F)](#-project-name--short-description)

## 🧰 Project name & short description

**Kitsub** is a .NET CLI tool for subtitle-first video workflows. It lets you inspect media, mux or burn subtitles, extract tracks, convert subtitle formats, and manage fonts. When available, it integrates with external tools like FFmpeg, MKVToolNix, and MediaInfo.

## 🏷 Badge

- License: MIT

## ✨ Features

- Media inspection and MediaInfo JSON reporting.
- Mux subtitles into MKV outputs.
- Burn-in subtitles into video outputs.
- Extract audio/video/subtitle tracks.
- Subtitle conversion utilities.
- Font attachment and font checks for MKV files.
- Diagnostics and external tool management (status, fetch, clean, doctor).

## 📦 Installation

### Requirements

- .NET SDK 10.0
- Optional: FFmpeg, MKVToolNix, and MediaInfo (Kitsub uses them if available)

### Build from source

```bash
dotnet build Kitsub.sln
```

## 🚀 Usage

```bash
kitsub --help
kitsub <command> --help
```

Run from source:

```bash
dotnet run --project src/Kitsub.Cli -- --help
```

## 🖼 Output example

Inspect a file (prints track metadata to the console):

```bash
kitsub inspect "INPUT_FILE"
```

Generate a MediaInfo JSON report:

```bash
kitsub inspect mediainfo "INPUT_FILE"
```

Mux subtitles into MKV:

```bash
kitsub mux --in "INPUT_MKV" --sub "SUB_FILE" --lang eng --title "English" --default --out "OUTPUT_MKV"
```

## 📁 Project structure

```
.
├─ src/
│  ├─ Kitsub.Cli/        # CLI app (commands and UI logic)
│  ├─ Kitsub.Core/       # Domain logic
│  └─ Kitsub.Tooling/    # External tool integration and management
├─ docs/                 # Documentation
├─ scripts/              # Helper scripts
├─ tests/                # Tests
├─ Instructions.md       # Command overview and examples
├─ TESTING.md            # Testing notes
└─ LICENSE.txt           # License
```

## 🤝 Contributing

- Review the documentation in `docs/` and `Instructions.md`.
- Please make changes in a separate branch and describe them in the commit.
- Before submitting, make sure the project builds.

## 📄 License

Licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).

## 👥 Authors / contact

- AstraidLabs (maintainer)
- Repository: https://github.com/AstraidLabs/Kitsub
