using FluentAssertions;
using Kitsub.Cli;
using Spectre.Console;
using Spectre.Console.Cli;
using Xunit;

namespace Kitsub.Cli.Tests;

public class ValidationHelpersTests
{
    [Fact]
    public void ValidateFileExists_WhenMissing_ReturnsError()
    {
        var result = ValidationHelpers.ValidateFileExists("/missing/file.txt", "Input file");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Input file not found: /missing/file.txt");
    }

    [Fact]
    public void ValidateFileExists_WhenEmpty_ReturnsError()
    {
        var result = ValidationHelpers.ValidateFileExists(" ", "Input file");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Input file is required.");
    }

    [Fact]
    public void ValidateFileExists_WhenFileExists_ReturnsSuccess()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var filePath = Path.Combine(tempDir.Path, "input.txt");
            File.WriteAllText(filePath, "payload");

            var result = ValidationHelpers.ValidateFileExists(filePath, "Input file");

            result.Should().BeEquivalentTo(ValidationResult.Success());
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateDirectoryExists_WhenMissing_ReturnsError()
    {
        var result = ValidationHelpers.ValidateDirectoryExists("/missing/directory", "Output folder");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Output folder not found: /missing/directory");
    }

    [Fact]
    public void ValidateDirectoryExists_WhenEmpty_ReturnsError()
    {
        var result = ValidationHelpers.ValidateDirectoryExists(null, "Output folder");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Output folder is required.");
    }

    [Fact]
    public void ValidateDirectoryExists_WhenDirectoryExists_ReturnsSuccess()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var result = ValidationHelpers.ValidateDirectoryExists(tempDir.Path, "Output folder");

            result.Should().BeEquivalentTo(ValidationResult.Success());
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
