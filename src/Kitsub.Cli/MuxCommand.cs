using Kitsub.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class MuxCommand : CommandBase<MuxCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        public string InputMkv { get; init; } = string.Empty;

        [CommandOption("--sub <FILE>")]
        public string[] Subtitles { get; init; } = Array.Empty<string>();

        [CommandOption("--lang <ISO>")]
        public string? Language { get; init; }

        [CommandOption("--title <NAME>")]
        public string? Title { get; init; }

        [CommandOption("--default")]
        public bool Default { get; init; }

        [CommandOption("--no-default")]
        public bool NoDefault { get; init; }

        [CommandOption("--forced")]
        public bool Forced { get; init; }

        [CommandOption("--no-forced")]
        public bool NoForced { get; init; }

        [CommandOption("--out <FILE>")]
        public string? Output { get; init; }

        public override ValidationResult Validate()
        {
            var inputValidation = ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            if (Subtitles.Length == 0)
            {
                return ValidationResult.Error("At least one subtitle file is required.");
            }

            foreach (var subtitle in Subtitles)
            {
                var subValidation = ValidationHelpers.ValidateFileExists(subtitle, "Subtitle file");
                if (!subValidation.Successful)
                {
                    return subValidation;
                }
            }

            if (Default && NoDefault)
            {
                return ValidationResult.Error("Use either --default or --no-default, not both.");
            }

            if (Forced && NoForced)
            {
                return ValidationResult.Error("Use either --forced or --no-forced, not both.");
            }

            return ValidationResult.Success();
        }
    }

    public MuxCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            var name = Path.GetFileNameWithoutExtension(settings.InputMkv);
            output = Path.Combine(Path.GetDirectoryName(settings.InputMkv) ?? string.Empty, $"{name}.kitsub.mkv");
        }

        var defaultFlag = settings.Default ? true : settings.NoDefault ? false : (bool?)null;
        var forcedFlag = settings.Forced ? true : settings.NoForced ? false : (bool?)null;

        var subtitles = settings.Subtitles
            .Select(sub => new SubtitleDescriptor(sub, settings.Language, settings.Title, defaultFlag, forcedFlag))
            .ToList();

        using var tooling = ToolingFactory.CreateTooling(settings, Console);
        await tooling.Service.MuxSubtitlesAsync(settings.InputMkv, subtitles, output, cancellationToken)
            .ConfigureAwait(false);
        Console.MarkupLine($"[green]Muxed subtitles into[/] {Markup.Escape(output)}");
        return 0;
    }
}
