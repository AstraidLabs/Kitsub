# Testing

## Run unit tests

```bash
dotnet test Kitsub.sln
```

## Integration tests (optional)

Integration tests are skipped by default. Enable them by setting the environment variable before running tests:

```bash
KITSUB_RUN_INTEGRATION=1 dotnet test Kitsub.sln
```

Unit tests do not require external binaries such as ffmpeg or mkvmerge.
