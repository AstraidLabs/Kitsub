using Kitsub.Core;
using Kitsub.Tooling;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

public sealed class InspectCommand : CommandBase<InspectCommand.Settings>
{
    public sealed class Settings : ToolSettings
    {
        [CommandArgument(0, "<FILE>")]
        public string FilePath { get; init; } = string.Empty;

        public override ValidationResult Validate()
        {
            return ValidationHelpers.ValidateFileExists(FilePath, "Input file");
        }
    }

    public InspectCommand(IAnsiConsole console) : base(console)
    {
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings)
    {
        var paths = ToolingFactory.BuildToolPaths(settings);
        if (settings.DryRun)
        {
            var runner = new CliExternalToolRunner(Console, settings.DryRun, settings.Verbose);
            if (Path.GetExtension(settings.FilePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                var mkvmerge = new MkvmergeClient(runner, paths);
                Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(settings.FilePath).Rendered)}[/]");
            }
            else
            {
                var ffprobe = new FfprobeClient(runner, paths);
                Console.MarkupLine($"[grey]{Markup.Escape(ffprobe.BuildProbeCommand(settings.FilePath).Rendered)}[/]");
            }

            return 0;
        }

        var service = ToolingFactory.CreateService(settings, Console);
        var result = await service.InspectAsync(settings.FilePath, context.GetCancellationToken()).ConfigureAwait(false);
        var info = result.Info;
        var isMkv = result.IsMkv;

        RenderTracks(info);
        if (isMkv && info.Attachments.Count > 0)
        {
            RenderAttachments(info.Attachments);
        }

        return 0;
    }

    private void RenderTracks(MediaInfo info)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn("Type");
        table.AddColumn("Index/Id");
        table.AddColumn("Codec");
        table.AddColumn("Language");
        table.AddColumn("Title");
        table.AddColumn("Default");
        table.AddColumn("Forced");
        table.AddColumn("Extra");

        foreach (var track in info.Tracks)
        {
            table.AddRow(
                track.Type.ToString(),
                track.Id?.ToString() ?? track.Index.ToString(),
                track.Codec,
                track.Language ?? "-",
                track.Title ?? "-",
                track.IsDefault ? "yes" : "no",
                track.IsForced ? "yes" : "no",
                FormatExtra(track));
        }

        Console.Write(table);
    }

    private void RenderAttachments(IReadOnlyList<AttachmentInfo> attachments)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn("File");
        table.AddColumn("Mime");
        table.AddColumn("Size");

        foreach (var attachment in attachments)
        {
            table.AddRow(attachment.FileName, attachment.MimeType, attachment.SizeBytes.ToString());
        }

        Console.MarkupLine("\n[bold]Attachments[/]");
        Console.Write(table);
    }

    private static string FormatExtra(TrackInfo track)
    {
        if (track.Extra.Count == 0)
        {
            return "-";
        }

        return string.Join(", ", track.Extra.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
