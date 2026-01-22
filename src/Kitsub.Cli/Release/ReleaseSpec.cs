// Summary: Defines the JSON specification for release mux workflows.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kitsub.Cli;

/// <summary>Represents a release mux specification loaded from JSON.</summary>
public sealed class ReleaseSpec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Gets the input MKV path.</summary>
    [JsonPropertyName("input")]
    public string Input { get; init; } = string.Empty;

    /// <summary>Gets the optional output MKV path.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; init; }

    /// <summary>Gets the optional fonts directory path.</summary>
    [JsonPropertyName("fontsDir")]
    public string? FontsDir { get; init; }

    /// <summary>Gets the optional strict-mode flag.</summary>
    [JsonPropertyName("strict")]
    public bool? Strict { get; init; }

    /// <summary>Gets the subtitle track specifications.</summary>
    [JsonPropertyName("subtitles")]
    public IReadOnlyList<ReleaseSubtitleSpec> Subtitles { get; init; } = Array.Empty<ReleaseSubtitleSpec>();

    /// <summary>Loads a release specification from a JSON file.</summary>
    /// <param name="path">The path to the JSON specification file.</param>
    /// <returns>The parsed release specification.</returns>
    public static ReleaseSpec Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ReleaseSpec>(json, JsonOptions)
                ?? throw new ValidationException($"Release spec is invalid: {path}. Fix: ensure the JSON matches the release spec schema.");
        }
        catch (JsonException ex)
        {
            throw new ValidationException($"Release spec is invalid JSON: {path}. Fix: correct the JSON syntax.", ex);
        }
    }

    /// <summary>Resolves relative paths in the spec against the spec file directory.</summary>
    /// <param name="specPath">The path to the spec file.</param>
    /// <returns>A new <see cref="ReleaseSpec"/> with absolute paths.</returns>
    public ReleaseSpec ResolvePaths(string specPath)
    {
        var baseDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? Environment.CurrentDirectory;
        var subtitles = Subtitles ?? Array.Empty<ReleaseSubtitleSpec>();

        return new ReleaseSpec
        {
            Input = ResolvePath(Input, baseDir),
            Output = string.IsNullOrWhiteSpace(Output) ? null : ResolvePath(Output, baseDir),
            FontsDir = string.IsNullOrWhiteSpace(FontsDir) ? null : ResolvePath(FontsDir, baseDir),
            Strict = Strict,
            Subtitles = subtitles
                .Select(sub => new ReleaseSubtitleSpec
                {
                    Path = ResolvePath(sub.Path, baseDir),
                    Lang = sub.Lang,
                    Title = sub.Title,
                    Default = sub.Default,
                    Forced = sub.Forced
                })
                .ToArray()
        };
    }

    private static string ResolvePath(string path, string baseDir)
    {
        return Path.GetFullPath(path, baseDir);
    }

    public ReleaseSpec Validate(string specPath)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            throw new ValidationException($"Release spec is missing input. Fix: add \"input\" to {specPath}.");
        }

        if (!File.Exists(Input))
        {
            throw new ValidationException($"Input MKV not found: {Input}. Fix: update the input path in {specPath}.");
        }

        if (Subtitles is null || Subtitles.Count == 0)
        {
            throw new ValidationException($"Release spec must contain at least one subtitle. Fix: add subtitles to {specPath}.");
        }

        var defaultCount = Subtitles.Count(sub => sub.Default == true);
        if (defaultCount > 1)
        {
            throw new ValidationException($"Release spec has multiple default subtitles. Fix: mark only one subtitle as default in {specPath}.");
        }

        var pathSet = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var subtitle in Subtitles)
        {
            if (string.IsNullOrWhiteSpace(subtitle.Path))
            {
                throw new ValidationException($"Release spec subtitle path is required. Fix: set \"path\" for each subtitle in {specPath}.");
            }

            var fullPath = Path.GetFullPath(subtitle.Path);
            if (!pathSet.Add(fullPath))
            {
                throw new ValidationException($"Release spec contains duplicate subtitle path: {fullPath}. Fix: remove duplicates in {specPath}.");
            }

            if (!File.Exists(fullPath))
            {
                throw new ValidationException($"Subtitle file not found: {fullPath}. Fix: update the path in {specPath}.");
            }

            var subtitleFormatValidation = ValidationHelpers.ValidateSubtitleFile(fullPath, "Subtitle file");
            if (!subtitleFormatValidation.Successful)
            {
                throw new ValidationException(subtitleFormatValidation.Message ?? $"Subtitle file is invalid: {fullPath}. Fix: re-export a valid subtitle file.");
            }

            var langValidation = ValidationHelpers.ValidateLanguageTag(subtitle.Lang, "Subtitle language");
            if (!langValidation.Successful)
            {
                throw new ValidationException(langValidation.Message ?? $"Subtitle language is invalid. Fix: update the language tag in {specPath}.");
            }

            var titleValidation = ValidationHelpers.ValidateTitle(subtitle.Title, "Subtitle title");
            if (!titleValidation.Successful)
            {
                throw new ValidationException(titleValidation.Message ?? $"Subtitle title is invalid. Fix: update the title in {specPath}.");
            }
        }

        return this;
    }
}

/// <summary>Represents a subtitle entry within a release spec.</summary>
public sealed class ReleaseSubtitleSpec
{
    /// <summary>Gets the subtitle file path.</summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets the optional ISO language tag.</summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; init; }

    /// <summary>Gets the optional subtitle title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Gets a value indicating whether the subtitle is default.</summary>
    [JsonPropertyName("default")]
    public bool? Default { get; init; }

    /// <summary>Gets a value indicating whether the subtitle is forced.</summary>
    [JsonPropertyName("forced")]
    public bool? Forced { get; init; }
}
