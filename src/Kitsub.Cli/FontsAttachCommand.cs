using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class FontsAttachCommand : CommandBase<FontsAttachCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandOption("--in <MKV>")]
        public string InputMkv { get; init; } = string.Empty;

        [CommandOption("--dir <DIR>")]
        public string FontsDir { get; init; } = string.Empty;

        [CommandOption("--out <FILE>")]
        public string? Output { get; init; }

        public override ValidationResult Validate()
        {
            var inputValidation = ValidationHelpers.ValidateFileExists(InputMkv, "Input MKV");
            if (!inputValidation.Successful)
            {
                return inputValidation;
            }

            var dirValidation = ValidationHelpers.ValidateDirectoryExists(FontsDir, "Fonts directory");
            if (!dirValidation.Successful)
            {
                return dirValidation;
            }

            if (MkvmergeMuxer.EnumerateFonts(FontsDir).Count == 0)
            {
                return ValidationResult.Error("No font files found in the directory.");
            }

            return ValidationResult.Success();
        }
    }

    public FontsAttachCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings)
    {
        var output = settings.Output;
        if (string.IsNullOrWhiteSpace(output))
        {
            var name = Path.GetFileNameWithoutExtension(settings.InputMkv);
            output = Path.Combine(Path.GetDirectoryName(settings.InputMkv) ?? string.Empty, $"{name}.fonts.mkv");
        }

        var service = ToolingFactory.CreateService(settings, Console);
        await service.AttachFontsAsync(settings.InputMkv, settings.FontsDir, output, context.GetCancellationToken()).ConfigureAwait(false);
        Console.MarkupLine($"[green]Attached fonts into[/] {Markup.Escape(output)}");
        return 0;
    }
}
