using System.Buffers.Binary;
using System.Text;

namespace PS4Remarry.Core;

public sealed class SfoWriter
{
    private readonly List<Entry> _entries = new();

    private sealed class Entry
    {
        public string Key = "";
        public string? StringValue;
        public uint? UIntValue;
        public int MaxLen;
    }

    private static int AlignUp4(int n)
    {
        if (n <= 0) return 4;
        return ((n + 3) / 4) * 4;
    }

    public void SetString(string key, string value, int maxLen = 0)
    {
        int needed = Encoding.UTF8.GetByteCount(value ?? "") + 1;
        if (maxLen <= 0) maxLen = needed;
        maxLen = AlignUp4(maxLen);

        if (maxLen < needed)
            throw new ArgumentException(
                $"maxLen {maxLen} is too small for '{key}' (needs {needed}).");

        _entries.RemoveAll(e => e.Key == key);
        _entries.Add(new Entry { Key = key, StringValue = value ?? "", MaxLen = maxLen });
    }

    public void SetUInt32(string key, uint value)
    {
        _entries.RemoveAll(e => e.Key == key);
        _entries.Add(new Entry { Key = key, UIntValue = value, MaxLen = 4 });
    }

    public void Write(string path)
    {
        _entries.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

        byte[] keyTableBytes = BuildKeyTable(out var keyOffsets);
        byte[] dataTableBytes = BuildDataTable(out var dataOffsets, out var paramLens);

        const int headerSize = 20;
        int indexTableSize = _entries.Count * 16;
        uint keyTableOffset = (uint)(headerSize + indexTableSize);
        uint dataTableOffset = keyTableOffset + (uint)keyTableBytes.Length;

        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            bw.Write(0x46535000);
            bw.Write(0x00000101);
            bw.Write(keyTableOffset);
            bw.Write(dataTableOffset);
            bw.Write((uint)_entries.Count);

            foreach (var e in _entries)
            {
                bw.Write(keyOffsets[e.Key]);   // LE uint16

                // param_fmt is BIG-ENDIAN in the SFO format.
                ushort fmt = e.UIntValue is not null ? (ushort)0x0404 : (ushort)0x0402;
                bw.Write((byte)((fmt >> 8) & 0xFF));
                bw.Write((byte)(fmt & 0xFF));

                bw.Write(paramLens[e.Key]);    // LE uint32
                bw.Write((uint)e.MaxLen);      // LE uint32
                bw.Write(dataOffsets[e.Key]);  // LE uint32
            }

            bw.Write(keyTableBytes);
            bw.Write(dataTableBytes);
        }

        File.WriteAllBytes(path, ms.ToArray());
    }

    private byte[] BuildKeyTable(out Dictionary<string, ushort> keyOffsets)
    {
        keyOffsets = new Dictionary<string, ushort>();
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        foreach (var e in _entries)
        {
            keyOffsets[e.Key] = (ushort)ms.Position;
            bw.Write(Encoding.UTF8.GetBytes(e.Key));
            bw.Write((byte)0);
        }

        while (ms.Position % 4 != 0)
            bw.Write((byte)0);

        return ms.ToArray();
    }

    private byte[] BuildDataTable(out Dictionary<string, uint> dataOffsets,
                              out Dictionary<string, uint> paramLens)
    {
        dataOffsets = new Dictionary<string, uint>();
        paramLens = new Dictionary<string, uint>();
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        foreach (var e in _entries)
        {
            dataOffsets[e.Key] = (uint)ms.Position;

            // Slot size must be a positive multiple of 4 — this is what the
            // format means by param_max_len, and the next entry's data_offset
            // is computed from this, not from the actual byte count.
            int slot = e.MaxLen;
            if (slot <= 0) slot = 4;
            if (slot % 4 != 0) slot = ((slot + 3) / 4) * 4;

            long start = ms.Position;

            if (e.UIntValue is uint u)
            {
                bw.Write(u);
                paramLens[e.Key] = 4;
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(e.StringValue ?? "");
                bw.Write(bytes);
                bw.Write((byte)0);
                paramLens[e.Key] = (uint)(bytes.Length + 1);
            }

            // Pad the entry out to its declared slot size. This is the missing
            // step that was producing wrong data_offsets and a truncated file.
            long written = ms.Position - start;
            for (long i = written; i < slot; i++)
                bw.Write((byte)0);
        }

        return ms.ToArray();
    }
}