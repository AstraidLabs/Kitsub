using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.SetApplicationName("kitsub");
            config.AddCommand<InspectCommand>("inspect").WithDescription("Inspect media file.");
            config.AddCommand<MuxCommand>("mux").WithDescription("Mux subtitles into MKV.");
            config.AddCommand<BurnCommand>("burn").WithDescription("Burn subtitles into video.");

            config.AddBranch("fonts", fonts =>
            {
                fonts.SetDescription("Font attachments.");
                fonts.AddCommand<FontsAttachCommand>("attach").WithDescription("Attach fonts to MKV.");
                fonts.AddCommand<FontsCheckCommand>("check").WithDescription("Check fonts in MKV.");
            });

            config.AddBranch("extract", extract =>
            {
                extract.SetDescription("Extract media streams.");
                extract.AddCommand<ExtractAudioCommand>("audio").WithDescription("Extract audio track.");
                extract.AddCommand<ExtractSubCommand>("sub").WithDescription("Extract subtitle track.");
                extract.AddCommand<ExtractVideoCommand>("video").WithDescription("Extract video track.");
            });

            config.AddBranch("convert", convert =>
            {
                convert.SetDescription("Convert subtitles.");
                convert.AddCommand<ConvertSubCommand>("sub").WithDescription("Convert subtitle file.");
            });
        });

        return await app.RunAsync(args).ConfigureAwait(false);
    }
}
