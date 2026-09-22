using System.Buffers.Binary;
using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Thrown for any condition that made the original sfo.cpp tool call exit(1).
/// </summary>
public sealed class SfoParseException : Exception
{
    public SfoParseException(string message) : base(message) { }
}

/// <summary>
/// One entry from the param.sfo index table.
/// </summary>
public sealed class SfoEntry
{
    public ushort KeyOffset;
    public ushort ParamFmt;
    public uint ParamLen;
    public uint ParamMaxLen;
    public uint DataOffset;
    public string Key = "";
}

/// <summary>
/// C# port of the read-only path of sfo.cpp (header/entries/key table/data table
/// loading, PS4 PKG param.sfo offset lookup, and the --debug dump).
/// </summary>
public static class SfoParser
{
    private const uint PkgMagic  = 1414415231;   // "\x7FCNT" - PS4 PKG file
    private const uint DiscMagic = 1128612691;   // "SCEC"    - PS4 disc param.sfo
    private const uint SfoMagic  = 1179865088;   // "\0PSF"   - PS4 param.sfo file

    private const uint ParamSfoEntryId   = 0x1000;
    private const uint Icon0PngEntryId   = 0x1200;
    private const uint Icon0_00PngEntryId = 0x1201;
    private const uint Pic0PngEntryId    = 0x1220;
    private const uint Pic1PngEntryId    = 0x1006;

    private static readonly Dictionary<uint, string> KnownEntryNames = new()
    {
        [0x00000001] = "digests", [0x00000010] = "entry_keys", [0x00000020] = "image_key",
        [0x00000080] = "general_digests", [0x00000100] = "metas", [0x00000200] = "entry_names",
        [0x00000400] = "license.dat", [0x00000401] = "license.info", [0x00000402] = "nptitle.dat",
        [0x00000403] = "npbind.dat", [0x00000404] = "selfinfo.dat", [0x00000406] = "imageinfo.dat",
        [0x00000407] = "target-deltainfo.dat", [0x00000408] = "origin-deltainfo.dat",
        [0x00000409] = "psreserved.dat",
        [ParamSfoEntryId] = "param.sfo", [0x1001] = "playgo-chunk.dat", [0x1002] = "playgo-chunk.sha",
        [0x1003] = "playgo-manifest.xml", [0x1004] = "pronunciation.xml", [0x1005] = "pronunciation.sig",
        [Pic1PngEntryId] = "pic1.png", [0x1007] = "pubtoolinfo.dat", [0x1008] = "app/playgo-chunk.dat",
        [0x1009] = "app/playgo-chunk.sha", [0x100A] = "app/playgo-manifest.xml",
        [0x100B] = "shareparam.json", [0x100C] = "shareoverlayimage.png", [0x100D] = "save_data.png",
        [0x100E] = "shareprivacyguardimage.png",
        [Icon0PngEntryId] = "icon0.png", [Pic0PngEntryId] = "pic0.png", [0x1240] = "snd0.at9",
        [0x1280] = "icon0.dds",
    };

    private static readonly (uint EntryId, string FileName)[] ImageCandidates =
    {
        (Icon0PngEntryId, "icon0.png"),
        (Pic1PngEntryId, "pic1.png"),
        (Pic0PngEntryId, "pic0.png"),
        (Icon0_00PngEntryId, "icon0_00.png"),
    };

    public static string Parse(string filePath, bool debug)
    {
        var sb = new StringBuilder();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        if (fs.Length < 4)
            throw new SfoParseException("File is too small to contain a valid magic number.");

        uint magic = br.ReadUInt32();

        string kind;
        if (magic == PkgMagic)
        {
            kind = "PS4 PKG";
            if (debug) PrintPkgFileTable(sb, br);
            long offset = GetPs4PkgOffset(br);
            fs.Seek(offset, SeekOrigin.Begin);
        }
        else if (magic == DiscMagic)
        {
            kind = "PS4 disc param.sfo";
            fs.Seek(0x800, SeekOrigin.Begin);
        }
        else if (magic == SfoMagic)
        {
            kind = "param.sfo";
            fs.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            throw new SfoParseException("Param.sfo magic number not found.");
        }

        var header = LoadHeader(br);
        var entries = LoadEntries(br, header.EntriesCount);
        byte[] keyTable = LoadKeyTable(br, header);
        ResolveKeys(entries, keyTable);
        byte[] dataTable = LoadDataTable(br, entries, header.EntriesCount);

        if (debug)
        {
            sb.AppendLine($"Detected file type: {kind}");
            sb.AppendLine();
            PrintHeader(sb, header);
            PrintEntries(sb, entries, keyTable, dataTable);
            PrintKeyTable(sb, keyTable);
            PrintDataTable(sb, dataTable);
            sb.AppendLine("----------------------------------------");
            sb.AppendLine();
        }

        PrintParams(sb, entries, keyTable, dataTable, header);
        return sb.ToString();
    }

    private sealed class SfoHeader
    {
        public uint Magic;
        public uint Version;
        public uint KeyTableOffset;
        public uint DataTableOffset;
        public uint EntriesCount;
    }

    private static SfoHeader LoadHeader(BinaryReader br)
    {
        try
        {
            return new SfoHeader
            {
                Magic = br.ReadUInt32(),
                Version = br.ReadUInt32(),
                KeyTableOffset = br.ReadUInt32(),
                DataTableOffset = br.ReadUInt32(),
                EntriesCount = br.ReadUInt32(),
            };
        }
        catch (EndOfStreamException)
        {
            throw new SfoParseException("Could not read header.");
        }
    }

    private static List<SfoEntry> LoadEntries(BinaryReader br, uint count)
    {
        var entries = new List<SfoEntry>((int)count);
        try
        {
            for (int i = 0; i < count; i++)
            {
                entries.Add(new SfoEntry
                {
                    KeyOffset = br.ReadUInt16(),
                    ParamFmt = br.ReadUInt16(),
                    ParamLen = br.ReadUInt32(),
                    ParamMaxLen = br.ReadUInt32(),
                    DataOffset = br.ReadUInt32(),
                });
            }
        }
        catch (EndOfStreamException)
        {
            throw new SfoParseException("Could not read index table entries.");
        }
        return entries;
    }

    private static byte[] LoadKeyTable(BinaryReader br, SfoHeader header)
    {
        long size = (long)header.DataTableOffset - header.KeyTableOffset;
        if (size < 0 || size > int.MaxValue)
            throw new SfoParseException("Could not read key table (invalid table offsets).");

        byte[] buf = new byte[size];
        if (size > 0 && br.Read(buf, 0, (int)size) != size)
            throw new SfoParseException("Could not read key table.");
        return buf;
    }

    private static void ResolveKeys(List<SfoEntry> entries, byte[] keyTable)
    {
        foreach (var e in entries)
            e.Key = ReadCString(keyTable, e.KeyOffset);
    }

    private static byte[] LoadDataTable(BinaryReader br, List<SfoEntry> entries, uint entriesCount)
    {
        long size = 0;
        if (entriesCount > 0)
        {
            var last = entries[(int)entriesCount - 1];
            size = (long)last.DataOffset + last.ParamMaxLen;
        }
        if (size < 0 || size > int.MaxValue)
            throw new SfoParseException("Could not read data table (invalid table size).");

        byte[] buf = new byte[size];
        if (size > 0 && br.Read(buf, 0, (int)size) != size)
            throw new SfoParseException("Could not read data table.");
        return buf;
    }

    private static void PrintPkgFileTable(StringBuilder sb, BinaryReader br)
    {
        var fs = br.BaseStream;

        fs.Seek(0x00C, SeekOrigin.Begin);
        uint pkgFileCount = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());

        fs.Seek(0x018, SeekOrigin.Begin);
        uint pkgTableOffset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());

        sb.AppendLine("=== PKG outer file table ===");
        sb.AppendLine($"File count: {pkgFileCount}, table offset: 0x{pkgTableOffset:X}");

        fs.Seek(pkgTableOffset, SeekOrigin.Begin);
        for (int i = 0; i < pkgFileCount; i++)
        {
            uint idRaw = br.ReadUInt32();
            uint filenameOffset = br.ReadUInt32();
            uint flags1 = br.ReadUInt32();
            uint flags2 = br.ReadUInt32();
            uint offset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
            uint size = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
            br.ReadUInt64();

            uint idLogical = BinaryPrimitives.ReverseEndianness(idRaw);
            string name = KnownEntryNames.TryGetValue(idLogical, out var n) ? n : "(unknown)";
            sb.AppendLine(
                $"  [{i}] id=0x{idLogical:X4} {name,-28} filename_offset=0x{filenameOffset:X}  " +
                $"flags1=0x{flags1:X8}  flags2=0x{flags2:X8}  offset=0x{offset:X}  size=0x{size:X}");
        }
        sb.AppendLine("=== end PKG outer file table ===");
        sb.AppendLine();
    }

    private static long GetPs4PkgOffset(BinaryReader br)
    {
        if (TryFindPkgTableEntry(br, ParamSfoEntryId, out uint offset, out _))
            return offset;

        throw new SfoParseException("Could not find a param.sfo file inside the PS4 PKG.");
    }

    private static bool TryFindPkgTableEntry(BinaryReader br, uint entryId, out uint offset, out uint size)
    {
        var fs = br.BaseStream;

        fs.Seek(0x00C, SeekOrigin.Begin);
        uint pkgFileCount = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());

        fs.Seek(0x018, SeekOrigin.Begin);
        uint pkgTableOffset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());

        fs.Seek(pkgTableOffset, SeekOrigin.Begin);

        uint rawCompareId = BinaryPrimitives.ReverseEndianness(entryId);

        for (int i = 0; i < pkgFileCount; i++)
        {
            uint id = br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            uint off = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
            uint sz = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
            br.ReadUInt64();

            if (id == rawCompareId)
            {
                offset = off;
                size = sz;
                return true;
            }
        }

        offset = 0;
        size = 0;
        return false;
    }

    public sealed class IconExtractionResult
    {
        public bool Found;
        public byte[]? Bytes;
        public string Status = "";
    }

    public sealed class CoverArtResult
    {
        public IconExtractionResult Icon0 = new();
        public IconExtractionResult Pic1 = new();
    }

    public static CoverArtResult ExtractCoverArt(string filePath)
    {
        var result = new CoverArtResult();
        const long scanWindow = 16 * 1024 * 1024;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            if (fs.Length < 4)
            {
                result.Icon0.Status = result.Pic1.Status = "File is too small to contain a valid magic number.";
                return result;
            }

            uint magic = br.ReadUInt32();
            if (magic != PkgMagic)
            {
                result.Icon0.Status = result.Pic1.Status = "Not a PKG file - icon0.png/pic1.png are only stored inside PKGs.";
                return result;
            }

            long paramSfoEnd = TryFindPkgTableEntry(br, ParamSfoEntryId, out uint sfoOffset, out uint sfoSize)
                ? (long)sfoOffset + sfoSize
                : -1;

            bool icon0Found = LocateAndRead(fs, br, Icon0PngEntryId, "icon0.png", paramSfoEnd, scanWindow, result.Icon0, out long icon0Offset, out long icon0Size);

            if (!icon0Found)
            {
                foreach (var (entryId, fileName) in ImageCandidates)
                {
                    if (entryId == Icon0PngEntryId) continue;
                    if (TryExtractCandidate(fs, br, entryId, fileName, isPreferredIcon: false, result.Icon0))
                    {
                        result.Icon0.Status = $"icon0.png not present - showing {fileName} instead. {result.Icon0.Status}";
                        break;
                    }
                }
                if (!result.Icon0.Found)
                    result.Icon0.Status = "No icon0.png (by table entry or signature scan), pic1.png, pic0.png or icon0_00.png found.";
            }

            long pic1ScanStart = icon0Found ? icon0Offset + icon0Size : paramSfoEnd;
            LocateAndRead(fs, br, Pic1PngEntryId, "pic1.png", pic1ScanStart, scanWindow, result.Pic1, out _, out _);

            return result;
        }
        catch (Exception ex)
        {
            result.Icon0.Status = result.Pic1.Status = $"Error reading cover art: {ex.Message}";
            return result;
        }
    }

    public static IconExtractionResult TryExtractIcon0(string filePath) => ExtractCoverArt(filePath).Icon0;
    public static IconExtractionResult TryExtractPic1(string filePath) => ExtractCoverArt(filePath).Pic1;

    private static bool LocateAndRead(FileStream fs, BinaryReader br, uint entryId, string fileName,
        long scanStart, long scanWindow, IconExtractionResult result, out long offset, out long size)
    {
        offset = 0;
        size = 0;

        if (TryFindPkgTableEntry(br, entryId, out uint tOffset, out uint tSize)
            && tSize > 0 && tOffset > 0 && (long)tOffset + tSize <= fs.Length)
        {
            offset = tOffset;
            size = tSize;
            if (ReadBytes(fs, offset, size, out byte[] buf))
            {
                result.Found = true;
                result.Bytes = buf;
                result.Status = $"{fileName} found at offset 0x{offset:X} ({size:N0} bytes).";
                return true;
            }
        }

        if (scanStart >= 0 && TryFindEmbeddedPng(fs, scanStart, scanWindow, out long pngOffset, out long pngSize))
        {
            offset = pngOffset;
            size = pngSize;
            if (ReadBytes(fs, offset, size, out byte[] buf))
            {
                result.Found = true;
                result.Bytes = buf;
                result.Status = $"{fileName} isn't in the file table, but was recovered by scanning for its PNG " +
                                 $"signature (offset 0x{offset:X}, {size:N0} bytes).";
                return true;
            }
        }

        if (result.Status.Length == 0)
            result.Status = $"No {fileName} entry found in the file table, and no PNG signature found nearby.";
        return false;
    }

    private static bool ReadBytes(FileStream fs, long offset, long size, out byte[] buf)
    {
        buf = Array.Empty<byte>();
        if (size <= 0 || offset <= 0 || offset + size > fs.Length) return false;
        fs.Seek(offset, SeekOrigin.Begin);
        buf = new byte[size];
        return fs.Read(buf, 0, (int)size) == size;
    }

    private static bool TryExtractCandidate(FileStream fs, BinaryReader br, uint entryId, string fileName, bool isPreferredIcon, IconExtractionResult result)
    {
        if (!TryFindPkgTableEntry(br, entryId, out uint offset, out uint size))
            return false;

        if (size == 0 || offset == 0 || (long)offset + size > fs.Length)
        {
            result.Status = $"{fileName} table entry looks invalid (offset={offset}, size={size}).";
            return false;
        }

        fs.Seek(offset, SeekOrigin.Begin);
        byte[] buf = new byte[size];
        if (fs.Read(buf, 0, (int)size) != size)
        {
            result.Status = $"Could not read {fileName} bytes from the file.";
            return false;
        }

        result.Found = true;
        result.Bytes = buf;
        result.Status = isPreferredIcon
            ? $"icon0.png found at offset 0x{offset:X} ({size:N0} bytes)."
            : $"{fileName} found at offset 0x{offset:X} ({size:N0} bytes).";
        return true;
    }

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static bool TryFindEmbeddedPng(FileStream fs, long searchStart, long searchLength, out long pngOffset, out long pngSize)
    {
        pngOffset = 0;
        pngSize = 0;

        if (searchStart < 0 || searchStart >= fs.Length) return false;
        long windowLen = Math.Min(searchLength, fs.Length - searchStart);
        if (windowLen < PngSignature.Length) return false;

        byte[] window = new byte[windowLen];
        fs.Seek(searchStart, SeekOrigin.Begin);
        int totalRead = 0;
        while (totalRead < windowLen)
        {
            int r = fs.Read(window, totalRead, (int)(windowLen - totalRead));
            if (r <= 0) break;
            totalRead += r;
        }

        int idx = IndexOfBytes(window, totalRead, PngSignature);
        if (idx < 0) return false;

        long start = searchStart + idx;
        long cursor = start + PngSignature.Length;
        Span<byte> hdr = stackalloc byte[8];

        while (true)
        {
            if (cursor + 8 > fs.Length) return false;
            fs.Seek(cursor, SeekOrigin.Begin);
            if (fs.Read(hdr) < 8) return false;

            uint chunkLen = BinaryPrimitives.ReadUInt32BigEndian(hdr.Slice(0, 4));
            bool isIend = hdr[4] == (byte)'I' && hdr[5] == (byte)'E' && hdr[6] == (byte)'N' && hdr[7] == (byte)'D';

            cursor += 8 + chunkLen + 4;

            if (chunkLen > 100_000_000 || cursor > fs.Length)
                return false;

            if (isIend)
            {
                pngOffset = start;
                pngSize = cursor - start;
                return true;
            }
        }
    }

    private static int IndexOfBytes(byte[] haystack, int haystackLen, byte[] needle)
    {
        for (int i = 0; i <= haystackLen - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    private static string ReadCString(byte[] table, int offset)
    {
        if (offset < 0 || offset >= table.Length) return "";
        int end = offset;
        while (end < table.Length && table[end] != 0) end++;
        return Encoding.UTF8.GetString(table, offset, end - offset);
    }

    private static void PrintParams(StringBuilder sb, List<SfoEntry> entries, byte[] keyTable, byte[] dataTable, SfoHeader header)
    {
        foreach (var e in entries)
        {
            switch (e.ParamFmt)
            {
                case 516:
                case 1024:
                    string s = ReadCString(dataTable, (int)e.DataOffset);
                    sb.AppendLine($"{e.Key}={s}");
                    break;
                case 1028:
                    uint value = BitConverter.ToUInt32(dataTable, (int)e.DataOffset);
                    sb.AppendLine($"{e.Key}=0x{value:X8}");
                    break;
                default:
                    sb.AppendLine($"{e.Key}=<unknown format 0x{e.ParamFmt:X4}>");
                    break;
            }
        }
    }

    private static void PrintHeader(StringBuilder sb, SfoHeader h)
    {
        sb.AppendLine("Header:");
        sb.AppendLine("Size: 20 (0x0014)");
        sb.AppendLine($".magic: {h.Magic} (0x{h.Magic:X8}) - (0x{BinaryPrimitives.ReverseEndianness(h.Magic):X8})");
        sb.AppendLine($".version: {h.Version} (0x{h.Version:X8}) - (0x{BinaryPrimitives.ReverseEndianness(h.Version):X8})");
        sb.AppendLine($".key_table_offset: {h.KeyTableOffset} (0x{h.KeyTableOffset:X8}) - (0x{BinaryPrimitives.ReverseEndianness(h.KeyTableOffset):X8})");
        sb.AppendLine($".data_table_offset: {h.DataTableOffset} (0x{h.DataTableOffset:X8}) - (0x{BinaryPrimitives.ReverseEndianness(h.DataTableOffset):X8})");
        sb.AppendLine($".entries_count: {h.EntriesCount} (0x{h.EntriesCount:X8}) - (0x{BinaryPrimitives.ReverseEndianness(h.EntriesCount):X8})");
        sb.AppendLine();
    }

    private static void PrintEntries(StringBuilder sb, List<SfoEntry> entries, byte[] keyTable, byte[] dataTable)
    {
        sb.AppendLine("Index table:");
        sb.AppendLine($"Size: {entries.Count * 16} (0x{entries.Count * 16:X4})");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            sb.AppendLine($"Entry {i}: (0x{i:X4})");
            sb.AppendLine($"  .key_offset: {e.KeyOffset} (0x{e.KeyOffset:X4}) - (0x{BinaryPrimitives.ReverseEndianness(e.KeyOffset):X4})-> \"{e.Key}\"");
            sb.AppendLine($"  .param_fmt: {e.ParamFmt} (0x{e.ParamFmt:X4}) - (0x{BinaryPrimitives.ReverseEndianness(e.ParamFmt):X4})");
            sb.AppendLine($"  .param_len: {e.ParamLen} (0x{e.ParamLen:X8}) - (0x{BinaryPrimitives.ReverseEndianness(e.ParamLen):X8})");
            sb.AppendLine($"  .param_max_len: {e.ParamMaxLen} (0x{e.ParamMaxLen:X8}) - (0x{BinaryPrimitives.ReverseEndianness(e.ParamMaxLen):X8})");
            string dataStr = e.ParamFmt switch
            {
                516 or 1024 => $"\"{ReadCString(dataTable, (int)e.DataOffset)}\"",
                1028 => $"0x{BitConverter.ToUInt32(dataTable, (int)e.DataOffset):X8}",
                _ => "<unknown>"
            };
            sb.AppendLine($"  .data_offset: {e.DataOffset} (0x{e.DataOffset:X8}) - (0x{BinaryPrimitives.ReverseEndianness(e.DataOffset):X8})-> {dataStr}");
        }
        sb.AppendLine();
    }

    private static void PrintKeyTable(StringBuilder sb, byte[] keyTable)
    {
        sb.AppendLine("Key table:");
        sb.AppendLine($"Size: {keyTable.Length} (0x{keyTable.Length:X4})");
        if (keyTable.Length > 0)
        {
            sb.AppendLine("Content:");
            var line = new StringBuilder();
            foreach (byte b in keyTable)
            {
                if (b >= 0x20 && b < 0x7F) line.Append((char)b);
                else line.Append($"'\\{b}'");
            }
            sb.AppendLine(line.ToString());
        }
        sb.AppendLine();
    }

    private static void PrintDataTable(StringBuilder sb, byte[] dataTable)
    {
        sb.AppendLine("Data table:");
        sb.AppendLine($"Size: {dataTable.Length} (0x{dataTable.Length:X4})");
        if (dataTable.Length > 0)
        {
            sb.AppendLine("Content:");
            HexPrint(sb, dataTable);
        }
        sb.AppendLine();
    }

    private static void HexPrint(StringBuilder sb, byte[] array)
    {
        sb.AppendLine("      0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f");
        int offset = 0;
        while (offset < array.Length)
        {
            sb.Append($"{offset:x4} ");
            int count = Math.Min(16, array.Length - offset);
            for (int i = 0; i < count; i++)
                sb.Append($"{array[offset + i]:x2} ");
            for (int i = count; i < 16; i++)
                sb.Append("   ");
            for (int i = 0; i < count; i++)
            {
                byte b = array[offset + i];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            sb.AppendLine();
            offset += 16;
        }
        if (array.Length > 64)
            sb.AppendLine("      0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f");
    }

    /// <summary>
    /// Convenience: parse a param.sfo (or PKG) and return key/value pairs directly.
    /// </summary>
    public static Dictionary<string, string> ParseToDictionary(string filePath, bool debug = false)
    {
        var text = Parse(filePath, debug);
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line.Substring(0, eq);
            var val = line.Substring(eq + 1);
            dict[key] = val;
        }
        return dict;
    }
}