namespace Kitsub.Tooling;

public sealed record ToolCommand(string Executable, IReadOnlyList<string> Arguments)
{
    public string Rendered => CommandLineRenderer.Render(Executable, Arguments);
}
