using System.Buffers.Binary;
using System.IO;

namespace PS4Remarry.Core;

/// <summary>
/// Port of hippie68's MarrySum.java from ps4-pkg-compatibility-checker.
///
/// Extracts the "marry digest" from a base game (or DLC) PKG. This is the
/// 32-byte SHA-256 value stored in the PKG's digest table at the index
/// matching the application (or DLC) entry in the entry table.
///
/// The same digest is what an update PKG records internally as its
/// "OriginGameDigest" — so if you can read both, you can verify that an
/// update was built for a specific base game.
///
/// Returns the digest as an uppercase hex string (64 chars), or "-" if the
/// file isn't a base-game/DLC PKG that this algorithm understands.
/// </summary>
public static class MarryDigestReader
{
    private const uint PkgMagic = 0x7F434E54;

    // Header field offsets, all read big-endian (Java's MappedByteBuffer
    // default byte order, which the original code relies on).
    private const int OffsetEntryCount   = 0x10;
    private const int OffsetTableOffset  = 0x18;
    private const int OffsetContentType  = 0x74;
    private const int OffsetContentFlags = 0x78;

    public static string GetChecksum(string pkgPath)
    {
        try
        {
            using var fs = new FileStream(pkgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            if (fs.Length < 0x1000)
                return "-";

            // 1. Magic
            fs.Seek(0, SeekOrigin.Begin);
            uint magic = ReadBE32(br);
            if (magic != PkgMagic)
                return "-";

            // 2. Header fields
            fs.Seek(OffsetEntryCount, SeekOrigin.Begin);
            uint entryCount = ReadBE32(br);

            fs.Seek(OffsetTableOffset, SeekOrigin.Begin);
            uint tableOffset = ReadBE32(br);

            fs.Seek(OffsetContentType, SeekOrigin.Begin);
            uint contentType = ReadBE32(br);
            if (contentType == 27)
                return "-"; // patch PKG — not a base game, nothing to hash here

            uint contentFlags = ReadBE32(br);

            // 3. Which entry holds the digest, based on content type bits.
            uint targetId;
            switch (contentFlags & 0x0F000000u)
            {
                case 0x0A000000u: targetId = 0x1001; break; // application (base game)
                case 0x02000000u: targetId = 0x1008; break; // additional content (DLC)
                default:          return "-";
            }

            // 4. Read the digest_table offset (big-endian) at tableOffset + 16.
            if (tableOffset + 20 > fs.Length) return "-";
            fs.Seek(tableOffset + 16, SeekOrigin.Begin);
            uint digestOffset = ReadBE32(br);

            // 5. Walk the entry table, find matching id, then read the
            //    same index from the digest table.
            if (tableOffset + entryCount * 32 > fs.Length) return "-";
            if (digestOffset + entryCount * 32 > fs.Length) return "-";

            for (uint i = 1; i < entryCount; i++)   // note: starts at 1, like the Java
            {
                fs.Seek(tableOffset + i * 32, SeekOrigin.Begin);
                uint id = ReadBE32(br);
                if (id != targetId) continue;

                fs.Seek(digestOffset + i * 32, SeekOrigin.Begin);
                byte[] raw = new byte[32];
                fs.ReadExactly(raw);
                return Convert.ToHexString(raw);
            }

            return "-";
        }
        catch
        {
            return "-";
        }
    }

    private static uint ReadBE32(BinaryReader br)
    {
        Span<byte> b = stackalloc byte[4];
        br.ReadExactly(b);
        return BinaryPrimitives.ReadUInt32BigEndian(b);
    }
}