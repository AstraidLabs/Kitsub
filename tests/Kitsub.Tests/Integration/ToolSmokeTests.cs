using System.Diagnostics;
using FluentAssertions;

namespace Kitsub.Tests.Integration;

public class ToolSmokeTests
{
    [IntegrationTest]
    public void Ffmpeg_ShouldReturnVersionWhenAvailable()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        process.ExitCode.Should().Be(0);
    }
}
