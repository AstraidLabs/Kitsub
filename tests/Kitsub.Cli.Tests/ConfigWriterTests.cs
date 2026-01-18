using FluentAssertions;
using Kitsub.Cli;
using Xunit;

namespace Kitsub.Cli.Tests;

public class ConfigWriterTests
{
    [Fact]
    public void WriteAtomic_WhenFileMissing_CreatesFileWithContent()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "kitsub.json");

            ConfigWriter.WriteAtomic(path, "{\"configVersion\":1}");

            File.Exists(path).Should().BeTrue();
            File.ReadAllText(path).Should().Be("{\"configVersion\":1}");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void WriteAtomic_WhenFileExists_CreatesBackup()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "kitsub.json");
            File.WriteAllText(path, "old");

            ConfigWriter.WriteAtomic(path, "new");

            File.ReadAllText(path).Should().Be("new");
            File.ReadAllText(path + ".bak").Should().Be("old");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kitsub_cli_tests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
