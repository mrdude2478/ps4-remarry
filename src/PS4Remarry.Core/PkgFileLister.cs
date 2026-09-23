using System.Buffers.Binary;
using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Lists the files inside a PS4 PKG by reading the file table directly,
/// without needing to extract or decrypt the package.
///
/// The file table is a list of 32-byte pkg_table_entry structures. Each entry
/// has an ID, flags, offset, size, and (for named files) a filename_offset
/// pointing into the entry_names table.
///
/// This is what orbis-pub-chk.exe does when you click "Extract files..." and
/// it shows the list of files with checkboxes.
/// </summary>
public static class PkgFileLister
{
    public sealed record PkgFileEntry(
        uint Id,
        string Name,
        long Offset,
        long Size,
        uint Flags1,
        uint Flags2);

    public sealed record ListResult(
        bool Success,
        string? Error,
        IReadOnlyList<PkgFileEntry> Files);

    public static ListResult ListFiles(string pkgPath)
    {
        try
        {
            using var fs = new FileStream(pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            if (fs.Length < 0x1000)
                return new ListResult(false, "File too small to be a PS4 PKG.", Array.Empty<PkgFileEntry>());

            // Magic check
            fs.Seek(0, SeekOrigin.Begin);
            uint magic = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
            if (magic != 0x7F434E54)
                return new ListResult(false, "Not a PS4 PKG (bad magic).", Array.Empty<PkgFileEntry>());

            // Header: entry count at 0x10, table offset at 0x18
            fs.Seek(0x10, SeekOrigin.Begin);
            uint entryCount = ReadBE32(br);

            fs.Seek(0x18, SeekOrigin.Begin);
            uint tableOffset = ReadBE32(br);

            if (entryCount == 0 || entryCount > 65536)
                return new ListResult(false, $"Invalid entry count: {entryCount}", Array.Empty<PkgFileEntry>());

            // Walk the entry table
            var entries = new List<PkgFileEntry>((int)entryCount);
            fs.Seek(tableOffset, SeekOrigin.Begin);

            for (uint i = 0; i < entryCount; i++)
            {
                uint id = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                uint filenameOffset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                uint flags1 = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                uint flags2 = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                uint offset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                uint size = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                br.ReadUInt64(); // padding

                // Try to resolve the filename. The entry_names table has
                // ID 0x200. Its entries are offset/size pairs into the data
                // area, but for named entries the PKG format stores the name
                // inline in a names table that we can read via the 0x200 entry.
                string name = ResolveFilename(fs, br, tableOffset, entryCount, filenameOffset, id);

                entries.Add(new PkgFileEntry(id, name, offset, size, flags1, flags2));
            }

            return new ListResult(true, null, entries);
        }
        catch (Exception ex)
        {
            return new ListResult(false, ex.Message, Array.Empty<PkgFileEntry>());
        }
    }

    private static string ResolveFilename(FileStream fs, BinaryReader br, uint tableOffset, uint entryCount, uint filenameOffset, uint id)
    {
        // For entries without a filename_offset, we fall back to a known
        // ID -> name mapping (same table your SfoParser uses for debug).
        if (filenameOffset == 0)
            return KnownEntryName(id);

        try
        {
            // The entry_names table lives at an entry with ID 0x200. We need
            // to find it and read the name at filenameOffset within it.
            // For simplicity, read a null-terminated string starting at
            // (tableOffset + filenameOffset) — the PKG stores names as
            // offset/string pairs where the offset is relative to the start
            // of the names table, and the names table itself follows the
            // entry table in most cases.
            //
            // The practical approach used by psdevwiki and LibOrbisPkg:
            // find the entry with id 0x200, get its offset/size, then the
            // name is at (namesTableOffset + filenameOffset).
            long namesOffset = FindNamesTableOffset(fs, br, tableOffset, entryCount);
            if (namesOffset < 0)
                return KnownEntryName(id);

            fs.Seek(namesOffset + filenameOffset, SeekOrigin.Begin);
            var sb = new StringBuilder();
            for (int i = 0; i < 512; i++)
            {
                int b = fs.ReadByte();
                if (b <= 0) break;
                sb.Append((char)b);
            }
            return sb.Length > 0 ? sb.ToString() : KnownEntryName(id);
        }
        catch
        {
            return KnownEntryName(id);
        }
    }

    private static long FindNamesTableOffset(FileStream fs, BinaryReader br, uint tableOffset, uint entryCount)
    {
        long saved = fs.Position;
        try
        {
            fs.Seek(tableOffset, SeekOrigin.Begin);
            for (uint i = 0; i < entryCount; i++)
            {
                uint id = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                br.ReadUInt32(); // filename_offset
                br.ReadUInt32(); // flags1
                br.ReadUInt32(); // flags2
                uint offset = BinaryPrimitives.ReverseEndianness(br.ReadUInt32());
                br.ReadUInt32(); // size
                br.ReadUInt64(); // padding

                if (id == 0x200)
                    return offset;
            }
        }
        finally
        {
            fs.Seek(saved, SeekOrigin.Begin);
        }
        return -1;
    }

    private static string KnownEntryName(uint id) => id switch
    {
        0x00000001 => "digests",
        0x00000010 => "entry_keys",
        0x00000020 => "image_key",
        0x00000080 => "general_digests",
        0x00000100 => "metas",
        0x00000200 => "entry_names",
        0x00000400 => "license.dat",
        0x00000401 => "license.info",
        0x00000402 => "nptitle.dat",
        0x00000403 => "npbind.dat",
        0x00000404 => "selfinfo.dat",
        0x00000406 => "imageinfo.dat",
        0x00000407 => "target-deltainfo.dat",
        0x00000408 => "origin-deltainfo.dat",
        0x00000409 => "psreserved.dat",
        0x00001000 => "param.sfo",
        0x00001001 => "playgo-chunk.dat",
        0x00001002 => "playgo-chunk.sha",
        0x00001003 => "playgo-manifest.xml",
        0x00001004 => "pronunciation.xml",
        0x00001005 => "pronunciation.sig",
        0x00001006 => "pic1.png",
        0x00001007 => "pubtoolinfo.dat",
        0x00001200 => "icon0.png",
        0x00001220 => "pic0.png",
        0x00001240 => "snd0.at9",
        0x00001280 => "icon0.dds",
        _ => $"(id=0x{id:X8})"
    };

    private static uint ReadBE32(BinaryReader br)
    {
        Span<byte> b = stackalloc byte[4];
        br.ReadExactly(b);
        return BinaryPrimitives.ReadUInt32BigEndian(b);
    }
}