using FluentAssertions;
using Kitsub.Core;
using Kitsub.Tooling.Provisioning;
using Xunit;

namespace Kitsub.Tests.Tooling;

public class ToolsManifestLoaderTests
{
    [Fact]
    public void Deserialize_ShouldLoadManifestFromJson()
    {
        var json = """
                   {
                     "toolsetVersion": "2024.09.15",
                     "rids": {
                       "win-x64": {
                         "ffmpeg": {
                           "version": "7.0",
                           "expectedSha256": "0000000000000000000000000000000000000000000000000000000000000000",
                           "archiveUrl": "https://example.com/ffmpeg.7z",
                           "sha256Url": "https://example.com/ffmpeg.sha256",
                           "archiveType": "7z",
                           "extractMap": {
                             "ffmpeg.exe": "bin/ffmpeg.exe",
                             "ffprobe.exe": "bin/ffprobe.exe"
                           }
                         },
                         "mkvtoolnix": {
                           "version": "80.0",
                           "expectedSha256": "0000000000000000000000000000000000000000000000000000000000000000",
                           "archiveUrl": "https://example.com/mkvtoolnix.7z",
                           "sha256Url": "https://example.com/mkvtoolnix.sha256",
                           "sha256Entry": "mkvtoolnix.7z",
                           "archiveType": "7z",
                           "extractMap": {
                             "mkvmerge.exe": "bin/mkvmerge.exe",
                             "mkvpropedit.exe": "bin/mkvpropedit.exe"
                           }
                         },
                         "mediainfo": {
                           "version": "24.0",
                           "expectedSha256": "0000000000000000000000000000000000000000000000000000000000000000",
                           "archiveUrl": "https://example.com/mediainfo.zip",
                           "archiveType": "zip",
                           "extractMap": {
                             "mediainfo.exe": "mediainfo.exe"
                           }
                         }
                       }
                     }
                   }
                   """;

        var manifest = ToolManifestLoader.Deserialize(json);
        ToolManifestLoader.ValidateManifest(manifest);

        manifest.ToolsetVersion.Should().Be("2024.09.15");
        manifest.Rids.Should().ContainKey("win-x64");
    }

    [Fact]
    public void ValidateManifest_ShouldThrowWhenRequiredFieldsMissing()
    {
        var json = """
                   {
                     "toolsetVersion": "",
                     "rids": {
                       "win-x64": {
                         "mkvtoolnix": {
                           "archiveUrl": "https://example.com/mkvtoolnix.7z",
                           "sha256Url": "https://example.com/mkvtoolnix.sha256",
                           "sha256Entry": "mkvtoolnix.7z",
                           "archiveType": "7z",
                           "extractMap": {
                             "mkvmerge.exe": "bin/mkvmerge.exe",
                             "mkvpropedit.exe": "bin/mkvpropedit.exe"
                           }
                         }
                       }
                     }
                   }
                   """;

        var manifest = ToolManifestLoader.Deserialize(json);
        var action = () => ToolManifestLoader.ValidateManifest(manifest);

        action.Should().Throw<ConfigurationException>()
            .WithMessage("Tools manifest missing toolsetVersion.");
    }

    [Fact]
    public void ParseSha256Content_ShouldHandleSingleTokenAndEntryFormats()
    {
        const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var singleToken = $"{hash}\n";
        var withEntry = $"{hash} mkvtoolnix.7z\n";

        var parsedSingle = ToolBundleManager.ParseSha256Content(singleToken, null);
        var parsedEntry = ToolBundleManager.ParseSha256Content(withEntry, "mkvtoolnix.7z");

        parsedSingle.Should().Be(hash);
        parsedEntry.Should().Be(hash);
    }
}
