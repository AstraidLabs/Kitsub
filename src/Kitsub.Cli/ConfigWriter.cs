// Summary: Handles atomic configuration file writes with backups.
using System.Text;

namespace Kitsub.Cli;

/// <summary>Provides helper methods for writing configuration files safely.</summary>
public static class ConfigWriter
{
    public static void WriteAtomic(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, encoding))
        {
            writer.Write(content);
            writer.Flush();
            stream.Flush(true);
        }

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }
}
