# CLI UX error/help notes

## Centralized error handling hook
- `Program.cs` configures `CommandApp` with `config.SetExceptionHandler(...)`, which routes parse/unknown-command errors to `CliAppExceptionHandler` for formatting and exit code mapping.
- `CommandBase` and `ConfigCommandBase` call `CommandErrorHandler.Handle(...)`, which now delegates usage-related errors to `CliErrorRenderer` so validation failures get tips and examples.

## Examples per command
- Examples live in `src/Kitsub.Cli/Help/ExamplesRegistry.cs` as a dictionary keyed by full command path (e.g., `"extract audio"`).
- `CliErrorRenderer` fetches examples from the registry and prints them for usage errors or the top suggestion when the command is unknown.

## Suggestion algorithm
- `CommandInventory.Suggest(...)` scores command-path candidates using a lightweight Levenshtein distance, then adds a prefix/contains bonus.
- Suggestions are capped at three and only shown when the score meets a 0.45 threshold to avoid noisy output.
