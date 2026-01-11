// Summary: Implements the CLI command that inspects media files and renders track metadata.
using Kitsub.Core;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Executes media inspection and renders track metadata to the console.</summary>
public sealed class InspectCommand : CommandBase<InspectCommand.Settings>
{
    private readonly ToolResolver _toolResolver;

    /// <summary>Defines command-line settings for media inspection.</summary>
    public sealed class Settings : ToolSettings
    {
        [CommandArgument(0, "<FILE>")]
        /// <summary>Gets the path to the media file to inspect.</summary>
        public string FilePath { get; init; } = string.Empty;

        /// <summary>Validates the provided settings for media inspection.</summary>
        /// <returns>A validation result indicating success or failure.</returns>
        public override ValidationResult Validate()
        {
            // Block: Validate the required input file before inspection.
            return ValidationHelpers.ValidateFileExists(FilePath, "Input file");
        }
    }

    /// <summary>Initializes the command with the console used for output.</summary>
    /// <param name="console">The console used to render command output.</param>
    public InspectCommand(IAnsiConsole console, ToolResolver toolResolver) : base(console)
    {
        // Block: Delegate console handling to the base command class.
        _toolResolver = toolResolver;
    }

    protected override async Task<int> ExecuteAsyncCore(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Block: Create tooling services scoped to this command execution.
        using var tooling = ToolingFactory.CreateTooling(settings, Console, _toolResolver);
        if (settings.DryRun)
        {
            if (Path.GetExtension(settings.FilePath).Equals(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                // Block: Render the MKV identify command without executing it.
                var mkvmerge = tooling.GetRequiredService<MkvmergeClient>();
                Console.MarkupLine($"[grey]{Markup.Escape(mkvmerge.BuildIdentifyCommand(settings.FilePath).Rendered)}[/]");
            }
            else
            {
                // Block: Render the ffprobe command without executing it.
                var ffprobe = tooling.GetRequiredService<FfprobeClient>();
                Console.MarkupLine($"[grey]{Markup.Escape(ffprobe.BuildProbeCommand(settings.FilePath).Rendered)}[/]");
            }

            return 0;
        }

        // Block: Inspect the media file and gather metadata for rendering.
        var service = tooling.Service;
        var result = await service.InspectAsync(settings.FilePath, cancellationToken).ConfigureAwait(false);
        var info = result.Info;
        var isMkv = result.IsMkv;

        // Block: Render track information for the inspected media file.
        RenderTracks(info);
        if (isMkv && info.Attachments.Count > 0)
        {
            // Block: Render attachments only for MKV files that contain attachments.
            RenderAttachments(info.Attachments);
        }

        return 0;
    }

    private void RenderTracks(MediaInfo info)
    {
        // Block: Build the table layout used to render track metadata.
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
            // Block: Add a row for each track with formatted metadata.
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

        // Block: Render the populated track table to the console.
        Console.Write(table);
    }

    private void RenderAttachments(IReadOnlyList<AttachmentInfo> attachments)
    {
        // Block: Build the table layout used to render attachment metadata.
        var table = new Table().RoundedBorder();
        table.AddColumn("File");
        table.AddColumn("Mime");
        table.AddColumn("Size");

        foreach (var attachment in attachments)
        {
            // Block: Add a row for each attachment with its key properties.
            table.AddRow(attachment.FileName, attachment.MimeType, attachment.SizeBytes.ToString());
        }

        // Block: Render a heading and the populated attachment table.
        Console.MarkupLine("\n[bold]Attachments[/]");
        Console.Write(table);
    }

    private static string FormatExtra(TrackInfo track)
    {
        if (track.Extra.Count == 0)
        {
            // Block: Return a placeholder when no extra metadata exists.
            return "-";
        }

        // Block: Format extra key-value metadata into a comma-separated string.
        return string.Join(", ", track.Extra.Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
