// Summary: Provides reusable validation helpers for CLI argument inputs.
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Provides helper methods for validating file system inputs.</summary>
public static class ValidationHelpers
{
    /// <summary>Validates that a file path is provided and points to an existing file.</summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="label">The label used in validation error messages.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateFileExists(string? path, string label)
    {
        // Block: Ensure a non-empty file path has been supplied.
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Error($"{label} is required.");
        }

        // Block: Return success only when the target file exists on disk.
        return File.Exists(path)
            ? ValidationResult.Success()
            : ValidationResult.Error($"{label} not found: {path}");
    }

    /// <summary>Validates that a directory path is provided and points to an existing directory.</summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="label">The label used in validation error messages.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateDirectoryExists(string? path, string label)
    {
        // Block: Ensure a non-empty directory path has been supplied.
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Error($"{label} is required.");
        }

        // Block: Return success only when the target directory exists on disk.
        return Directory.Exists(path)
            ? ValidationResult.Success()
            : ValidationResult.Error($"{label} not found: {path}");
    }
}
