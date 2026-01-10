using Spectre.Console.Cli;

namespace Kitsub.Cli;

public static class ValidationHelpers
{
    public static ValidationResult ValidateFileExists(string? path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Error($"{label} is required.");
        }

        return File.Exists(path)
            ? ValidationResult.Success()
            : ValidationResult.Error($"{label} not found: {path}");
    }

    public static ValidationResult ValidateDirectoryExists(string? path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Error($"{label} is required.");
        }

        return Directory.Exists(path)
            ? ValidationResult.Success()
            : ValidationResult.Error($"{label} not found: {path}");
    }
}
