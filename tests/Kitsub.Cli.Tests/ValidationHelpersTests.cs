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
        result.Message.Should().Be("Input file not found: /missing/file.txt. Fix: provide an existing Input file path.");
    }

    [Fact]
    public void ValidateFileExists_WhenEmpty_ReturnsError()
    {
        var result = ValidationHelpers.ValidateFileExists(" ", "Input file");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Input file is required. Fix: provide a valid Input file path.");
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
        result.Message.Should().Be("Output folder not found: /missing/directory. Fix: create the directory or provide a valid path.");
    }

    [Fact]
    public void ValidateDirectoryExists_WhenEmpty_ReturnsError()
    {
        var result = ValidationHelpers.ValidateDirectoryExists(null, "Output folder");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("Output folder is required. Fix: provide an existing Output folder.");
    }

    [Fact]
    public void ValidateOutputPath_WhenOutputExistsWithoutForce_ReturnsError()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var outputPath = Path.Combine(tempDir.Path, "output.mkv");
            File.WriteAllText(outputPath, "payload");

            var result = ValidationHelpers.ValidateOutputPath(outputPath, "Output file", allowCreateDirectory: true, allowOverwrite: false);

            result.Successful.Should().BeFalse();
            result.Message.Should().Be($"Output file already exists: {Path.GetFullPath(outputPath)}. Fix: pass --force to overwrite or choose another output path.");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateOutputPath_WhenOutputExistsWithForce_ReturnsSuccess()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var outputPath = Path.Combine(tempDir.Path, "output.mkv");
            File.WriteAllText(outputPath, "payload");

            var result = ValidationHelpers.ValidateOutputPath(outputPath, "Output file", allowCreateDirectory: true, allowOverwrite: true);

            result.Should().BeEquivalentTo(ValidationResult.Success());
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateOutputPath_WhenOutputMatchesInput_ReturnsError()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "input.mkv");
            File.WriteAllText(path, "payload");

            var result = ValidationHelpers.ValidateOutputPath(path, "Output file", allowCreateDirectory: true, allowOverwrite: true, inputPath: path);

            result.Successful.Should().BeFalse();
            result.Message.Should().Be("Output file must be different from the input path. Fix: choose a different output file.");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateOutputPath_WhenDirectoryMissingAndCreateAllowed_CreatesDirectory()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var outputDir = Path.Combine(tempDir.Path, "nested");
            var outputPath = Path.Combine(outputDir, "output.mkv");

            var result = ValidationHelpers.ValidateOutputPath(outputPath, "Output file", allowCreateDirectory: true, allowOverwrite: false);

            result.Should().BeEquivalentTo(ValidationResult.Success());
            Directory.Exists(outputDir).Should().BeTrue();
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateSubtitleFile_WhenExtensionUnsupported_ReturnsError()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "subtitle.txt");
            File.WriteAllText(path, "payload");

            var result = ValidationHelpers.ValidateSubtitleFile(path, "Subtitle file");

            result.Successful.Should().BeFalse();
            result.Message.Should().Be("Subtitle file must be .srt, .ass, or .ssa. Fix: re-export a valid subtitle file.");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateSubtitleFile_WhenSrtMissingTiming_ReturnsError()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "subtitle.srt");
            File.WriteAllText(path, "Not a subtitle file");

            var result = ValidationHelpers.ValidateSubtitleFile(path, "Subtitle file");

            result.Successful.Should().BeFalse();
            result.Message.Should().Be("Subtitle file does not look like a valid SRT file. Fix: re-export a valid SRT subtitle.");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateSubtitleFile_WhenAssMissingDialogue_ReturnsError()
    {
        var tempDir = TempDirectory.Create();
        try
        {
            var path = Path.Combine(tempDir.Path, "subtitle.ass");
            File.WriteAllText(path, "[Script Info]\nTitle=Test");

            var result = ValidationHelpers.ValidateSubtitleFile(path, "Subtitle file");

            result.Successful.Should().BeFalse();
            result.Message.Should().Be("Subtitle file does not look like a valid ASS/SSA file. Fix: re-export a valid ASS/SSA subtitle.");
        }
        finally
        {
            tempDir.Dispose();
        }
    }

    [Fact]
    public void ValidateSubtitleConversion_WhenUnsupportedPair_ReturnsError()
    {
        var result = ValidationHelpers.ValidateSubtitleConversion("input.ass", "output.srt");

        result.Successful.Should().BeFalse();
        result.Message.Should().Be("ASS to SRT conversion is not supported reliably. Fix: keep subtitles in ASS or export from your editor.");
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
