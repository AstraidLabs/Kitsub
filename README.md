# 🦊 Kitsub

[![CI](https://github.com/AstraidLabs/Kitsub/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/AstraidLabs/Kitsub/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/AstraidLabs/Kitsub?label=license)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Type](https://img.shields.io/badge/Type-CLI-2F6F8F)](#-overview)

🎬 **Video & subtitle build tool**

Kitsub is a CLI tool for building video outputs with burned-in or muxed subtitles.
It integrates with external tools such as FFmpeg, MKVToolNix, and MediaInfo when available.

## 🧱 Overview

- .NET CLI packaged as a tool with the command name `kitsub`.
- Supports inspection, muxing, burning, extraction, conversion, diagnostics, and tool management.
- Provides subtitle track selection and font checks for MKV files.
- Integrates with external tools via configured paths and optional provisioning.
- Can generate MediaInfo JSON reports for media files.

## 🧩 Capabilities

- Inspection and reporting (track metadata and MediaInfo JSON reports).
- Muxing subtitles into MKV outputs.
- Burning subtitles into video outputs.
- Extraction of audio, video, and subtitle tracks.
- Subtitle conversion utilities.
- Font attachment and font checks for MKV files.
- Diagnostics and tool management (status, fetch, clean, doctor).

## 🚀 Usage

```bash
kitsub --help
kitsub <command> --help
```

For examples, see [Instructions.md](Instructions.md).

## 📚 Documentation

- [Instructions.md](Instructions.md)
- [docs/README.md](docs/README.md)
- [TESTING.md](TESTING.md)
- [docs/TESTING.md](docs/TESTING.md)
- [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
- [LICENSE.txt](LICENSE.txt)
- [REPORT.md](REPORT.md)

## 🛠️ Project status

Open-source project.

## 📄 License

MIT License. See [LICENSE.txt](LICENSE.txt).
