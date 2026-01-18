using FluentAssertions;
using Kitsub.Cli;
using Serilog.Events;
using Xunit;

namespace Kitsub.Cli.Tests;

public class LogLevelParserTests
{
    [Theory]
    [InlineData("trace", LogEventLevel.Verbose)]
    [InlineData("TRACE", LogEventLevel.Verbose)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("info", LogEventLevel.Information)]
    [InlineData("warn", LogEventLevel.Warning)]
    [InlineData("error", LogEventLevel.Error)]
    public void Parse_KnownValue_ReturnsExpectedLevel(string value, LogEventLevel expected)
    {
        var result = LogLevelParser.Parse(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrNull_ReturnsInformation(string? value)
    {
        var result = LogLevelParser.Parse(value);

        result.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Parse_UnknownValue_ThrowsValidationException()
    {
        var act = () => LogLevelParser.Parse("verbose");

        act.Should().Throw<ValidationException>()
            .WithMessage("Unknown log level: verbose");
    }
}
