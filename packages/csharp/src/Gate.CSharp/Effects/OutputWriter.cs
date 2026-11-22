using System.IO;
using System.Text;
using Gate.CSharp.Model;

namespace Gate.CSharp.Effects;

/// <summary>Writes the files a command produced. The only writer of files.</summary>
internal static class OutputWriter
{
    public static void Write(Report report)
    {
        foreach (var file in report.Files)
        {
            var path = Path.Combine(report.Directory, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? report.Directory);
            File.WriteAllText(path, file.Text, new UTF8Encoding(file.ByteOrderMark > 0));
        }
    }
}
