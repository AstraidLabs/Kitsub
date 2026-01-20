# Kitsub

Video & subtitle build tool for repeatable, automated outputs.

## What it is

Kitsub is a CLI-first tool for building video outputs with subtitles in a repeatable,
automation-friendly way. It drives external tooling such as FFmpeg/ffprobe, MKVToolNix
(mkvmerge/mkvpropedit), and MediaInfo when available or provisioned. Tool provisioning
is Windows-only; on other platforms, provide tool paths or use tools on PATH.

## Kitsub is / is not

**Kitsub is:**
- A CLI-first video & subtitle build tool.
- Automation-friendly and repeatable by design.
- Focused on workflows that compose well in scripts.

**Kitsub is not:**
- A subtitle editor (e.g., Aegisub).
- A GUI application.
- An all-in-one solution for every media workflow.

## Quick start

```powershell
kitsub --help
kitsub <command> --help
```

### Build from source

```powershell
dotnet build Kitsub.sln

dotnet publish src/Kitsub.Cli -c Release -r win-x64 --self-contained false
```

For usage examples, see [Instructions.md](Instructions.md).

## Documentation

- [Instructions.md](Instructions.md) — practical usage and examples.
- [docs/README.md](docs/README.md) — additional CLI notes and release workflow details.
- [TESTING.md](TESTING.md) — test commands.
- [LICENSE.txt](LICENSE.txt)

## Maintenance policy

Maintained by a single author.

- No SLA.
- Focus: correctness, stability, maintainability.
- Best-effort support.

**Reporting bugs**
- OS + version
- Kitsub version/commit
- Full command line used
- Input details (safe to share)
- Logs/output
- Expected vs. actual behavior

## License

MIT License. See [LICENSE.txt](LICENSE.txt).
