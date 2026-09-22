using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Reads the 9-byte TITLE_ID (e.g. "CUSA12345") that lives at offset 0x47
/// of a PS4 PKG. Replaces the old sfk.exe partcopy call.
/// </summary>
public static class PkgTitleIdReader
{
    private const long TitleIdOffset = 0x47;
    private const int  TitleIdLength = 9;

    public static string ReadTitleId(string pkgPath)
    {
        if (string.IsNullOrWhiteSpace(pkgPath))
            throw new ArgumentException("PKG path is empty.", nameof(pkgPath));
        if (!File.Exists(pkgPath))
            throw new FileNotFoundException("PKG not found.", pkgPath);

        using var fs = new FileStream(
            pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, FileOptions.RandomAccess);

        if (fs.Length < TitleIdOffset + TitleIdLength)
            throw new InvalidDataException("File too small to be a PS4 PKG.");

        fs.Seek(TitleIdOffset, SeekOrigin.Begin);
        Span<byte> buf = stackalloc byte[TitleIdLength];
        fs.ReadExactly(buf);

        var id = Encoding.ASCII.GetString(buf).Trim('\0', ' ');

        if (id.Length != 9 || !id.All(char.IsLetterOrDigit))
            throw new InvalidDataException(
                $"Unexpected TITLE_ID '{id}' at 0x47 in '{Path.GetFileName(pkgPath)}'.");

        return id;
    }
}