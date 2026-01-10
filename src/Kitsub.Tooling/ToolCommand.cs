// Summary: Represents an external tool invocation with executable and argument details.
namespace Kitsub.Tooling;

/// <summary>Encapsulates a command-line invocation for an external tool.</summary>
/// <param name="Executable">The tool executable path or name.</param>
/// <param name="Arguments">The arguments passed to the tool.</param>
public sealed record ToolCommand(string Executable, IReadOnlyList<string> Arguments)
{
    /// <summary>Gets the rendered command-line string for the tool invocation.</summary>
    public string Rendered => ExternalToolRunner.RenderCommandLine(Executable, Arguments);
}
