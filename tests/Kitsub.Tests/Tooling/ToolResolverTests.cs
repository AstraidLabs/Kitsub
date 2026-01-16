using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Kitsub.Tooling;
using Kitsub.Tooling.Provisioning;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Kitsub.Tests.Tooling;

[Collection("ToolResolver")]
public class ToolResolverTests
{
    [Fact]
    public void Resolve_ShouldPreferOverridesWhenPresent()
    {
        var (resolver, ridEntry, _) = CreateResolver();
        var bundledRoot = CreateBundledToolset(ridEntry);
        var overridePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "ffmpeg.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(overridePath)!);
        File.WriteAllText(overridePath, string.Empty);

        try
        {
            var overrides = new ToolOverrides(overridePath, null, null, null, null);
            var options = new ToolResolveOptions { PreferBundled = true };

            var result = resolver.Resolve(overrides, options);

            result.Ffmpeg.Source.Should().Be(ToolSource.Override);
            result.Ffmpeg.Path.Should().Be(Path.GetFullPath(overridePath));
            result.Ffprobe.Source.Should().Be(ToolSource.Bundled);
        }
        finally
        {
            CleanupDirectory(bundledRoot);
            CleanupDirectory(Path.GetDirectoryName(overridePath));
        }
    }

    [Fact]
    public void Resolve_ShouldUseBundledWhenPreferredAndAvailable()
    {
        var (resolver, ridEntry, _) = CreateResolver();
        var bundledRoot = CreateBundledToolset(ridEntry);

        try
        {
            var options = new ToolResolveOptions { PreferBundled = true };

            var result = resolver.Resolve(new ToolOverrides(null, null, null, null, null), options);

            result.Ffmpeg.Source.Should().Be(ToolSource.Bundled);
            result.Ffprobe.Source.Should().Be(ToolSource.Bundled);
            result.Mkvmerge.Source.Should().Be(ToolSource.Bundled);
            result.Mkvpropedit.Source.Should().Be(ToolSource.Bundled);
            Path.IsPathRooted(result.Ffmpeg.Path).Should().BeTrue();
        }
        finally
        {
            CleanupDirectory(bundledRoot);
        }
    }

    [Fact]
    public void Resolve_ShouldUseCacheWhenBundledMissingAndPreferPathFalse()
    {
        var (resolver, ridEntry, toolsetVersion) = CreateResolver();
        var cacheRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        CreateCachedToolset(ridEntry, toolsetVersion, cacheRoot, out var toolsetRoot);

        try
        {
            var options = new ToolResolveOptions { PreferBundled = false, PreferPath = false, ToolsCacheDir = cacheRoot };

            var result = resolver.Resolve(new ToolOverrides(null, null, null, null, null), options);

            result.Ffmpeg.Source.Should().Be(ToolSource.Cache);
            result.Mkvmerge.Source.Should().Be(ToolSource.Cache);
            result.Ffmpeg.Path.Should().StartWith(toolsetRoot);
        }
        finally
        {
            CleanupDirectory(cacheRoot);
        }
    }

    [Fact]
    public void Resolve_ShouldFallBackToPathWhenNothingAvailable()
    {
        var (resolver, _, _) = CreateResolver();
        var options = new ToolResolveOptions { PreferBundled = false, PreferPath = true };

        var result = resolver.Resolve(new ToolOverrides(null, null, null, null, null), options);

        result.Ffmpeg.Source.Should().Be(ToolSource.Path);
        result.Ffmpeg.Path.Should().Be("ffmpeg");
    }

    private static (ToolResolver Resolver, ToolsManifestRid RidEntry, string ToolsetVersion) CreateResolver()
    {
        var manifestLoader = new ToolManifestLoader(Substitute.For<ILogger<ToolManifestLoader>>());
        var manifest = manifestLoader.Load();
        manifest.Rids.Should().ContainKey("win-x64");
        var ridEntry = manifest.Rids["win-x64"];

        var bundleManager = new ToolBundleManager(
            manifestLoader,
            new ToolCachePaths(Substitute.For<ILogger<ToolCachePaths>>()),
            Substitute.For<ILogger<ToolBundleManager>>());

        var ridDetector = Substitute.For<WindowsRidDetector>(Substitute.For<ILogger<WindowsRidDetector>>());
        ridDetector.IsWindows.Returns(true);
        ridDetector.GetRuntimeRid().Returns("win-x64");

        var resolver = new ToolResolver(bundleManager, ridDetector, Substitute.For<ILogger<ToolResolver>>());
        return (resolver, ridEntry, manifest.ToolsetVersion);
    }

    private static string CreateBundledToolset(ToolsManifestRid ridEntry)
    {
        var baseDirectory = Path.Combine(AppContext.BaseDirectory, "tools", "win-x64");
        CreateToolFiles(ridEntry, baseDirectory);
        return Path.Combine(AppContext.BaseDirectory, "tools");
    }

    private static void CreateCachedToolset(ToolsManifestRid ridEntry, string toolsetVersion, string cacheRoot, out string toolsetRoot)
    {
        toolsetRoot = Path.Combine(cacheRoot, "win-x64", toolsetVersion);
        CreateToolFiles(ridEntry, toolsetRoot);
        WriteHashManifest(toolsetRoot);
    }

    private static void CreateToolFiles(ToolsManifestRid ridEntry, string baseDirectory)
    {
        CreateToolFile(ResolveToolPath(ridEntry.Ffmpeg!, "ffmpeg.exe", baseDirectory, "ffmpeg"));
        CreateToolFile(ResolveToolPath(ridEntry.Ffmpeg!, "ffprobe.exe", baseDirectory, "ffmpeg"));
        CreateToolFile(ResolveToolPath(ridEntry.Mkvtoolnix!, "mkvmerge.exe", baseDirectory, "mkvtoolnix"));
        CreateToolFile(ResolveToolPath(ridEntry.Mkvtoolnix!, "mkvpropedit.exe", baseDirectory, "mkvtoolnix"));
        CreateToolFile(ResolveToolPath(ridEntry.Mediainfo!, "mediainfo.exe", baseDirectory, "mediainfo"));
    }

    private static string ResolveToolPath(ToolArchiveDefinition definition, string key, string baseDirectory, string toolRoot)
    {
        var relative = definition.ExtractMap[key].Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(baseDirectory, toolRoot, relative);
    }

    private static void CreateToolFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
    }

    private static void WriteHashManifest(string toolsetRoot)
    {
        var files = Directory.EnumerateFiles(toolsetRoot, "*.exe", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(toolsetRoot, path), ComputeSha256);

        var manifest = new { Files = files };
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(toolsetRoot, "tool-hashes.json"), json);
    }

    private static string ComputeSha256(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void CleanupDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        Directory.Delete(path, recursive: true);
    }
}

[CollectionDefinition("ToolResolver", DisableParallelization = true)]
public class ToolResolverCollection
{
}
