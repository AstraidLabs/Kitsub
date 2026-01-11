namespace Kitsub.Tests.Helpers;

public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kitsub-tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateFile(string relativePath, string? contents = null)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        System.IO.File.WriteAllText(fullPath, contents ?? string.Empty);
        return fullPath;
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
        {
            System.IO.Directory.Delete(Path, recursive: true);
        }
    }
}
