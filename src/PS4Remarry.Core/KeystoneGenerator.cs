using System.Security.Cryptography;
using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Builds and verifies PS4 keystone files (96 bytes) for a given 32-char
/// ASCII passcode, using the public Sony secrets from the PS4 DevWiki.
///
/// Layout of a keystone:
///   0x00-0x1F  constant header ("keystone", version 2.0.1.0, ...)
///   0x20-0x3F  passcode fingerprint = HMAC-SHA256(passcode) keyed by
///              the passcode secret
///   0x40-0x5F  integrity HMAC = HMAC-SHA256(header||fingerprint) keyed
///              by the keystone secret
/// </summary>
public static class KeystoneGenerator
{
    public const int KeystoneLength = 96;
    public const int PasscodeLength = 32;

    // Public Sony keystone secrets (from PS4 DevWiki).
    private const string PasscodeSecretHex =
        "C74405F67424BA342BC1276251BBC2F555F16025B6A1B6714780DBAEC852FA2F";
    private const string KsSecretHex =
        "783D6F3AE91C0E0712FCAAB7950BDE06855CF7A22DCDBDE127E9BFCBAD0FF0FE";

    public static readonly byte[] Header =
    {
        0x6B, 0x65, 0x79, 0x73, 0x74, 0x6F, 0x6E, 0x65,
        0x02, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    /// <summary>
    /// Builds a 96-byte keystone from a 32-character ASCII passcode.
    /// Throws <see cref="ArgumentException"/> if the passcode is the wrong
    /// length or contains non-ASCII characters.
    /// </summary>
    public static byte[] Build(string passcode)
    {
        ValidatePasscode(passcode);

        byte[] passcodeSecret = Convert.FromHexString(PasscodeSecretHex);
        byte[] ksSecret       = Convert.FromHexString(KsSecretHex);
        byte[] passcodeBytes  = Encoding.ASCII.GetBytes(passcode);

        // 1) Fingerprint = HMAC-SHA256(passcode) keyed by passcode secret.
        byte[] fingerprint;
        using (var hmac = new HMACSHA256(passcodeSecret))
            fingerprint = hmac.ComputeHash(passcodeBytes);

        // 2) first64 = header || fingerprint.
        byte[] first64 = new byte[64];
        Buffer.BlockCopy(Header,      0, first64,  0, 32);
        Buffer.BlockCopy(fingerprint, 0, first64, 32, 32);

        // 3) trailer = HMAC-SHA256(first64) keyed by ks secret.
        byte[] trailer;
        using (var hmac = new HMACSHA256(ksSecret))
            trailer = hmac.ComputeHash(first64);

        byte[] keystone = new byte[KeystoneLength];
        Buffer.BlockCopy(first64, 0, keystone,  0, 64);
        Buffer.BlockCopy(trailer, 0, keystone, 64, 32);
        return keystone;
    }

    /// <summary>
    /// Returns true if the given passcode's fingerprint matches the
    /// 32 bytes stored at 0x20..0x3F in the supplied keystone.
    /// </summary>
    public static bool Matches(string passcode, byte[] keystone)
    {
        if (keystone is null || keystone.Length != KeystoneLength)
            return false;

        ValidatePasscode(passcode);

        byte[] actual = ComputeFingerprint(passcode);
        for (int i = 0; i < 32; i++)
            if (actual[i] != keystone[32 + i])
                return false;
        return true;
    }

    /// <summary>
    /// Recomputes the integrity HMAC over the first 64 bytes and compares
    /// it against the trailer. If this fails the keystone is corrupt or
    /// was never produced by this algorithm.
    /// </summary>
    public static bool VerifyIntegrity(byte[] keystone)
    {
        if (keystone is null || keystone.Length != KeystoneLength)
            return false;

        // Check header.
        for (int i = 0; i < 8; i++)
            if (keystone[i] != Header[i]) return false;

        byte[] ksSecret = Convert.FromHexString(KsSecretHex);
        byte[] first64  = new byte[64];
        Buffer.BlockCopy(keystone, 0, first64, 0, 64);

        byte[] expected;
        using (var hmac = new HMACSHA256(ksSecret))
            expected = hmac.ComputeHash(first64);

        for (int i = 0; i < 32; i++)
            if (expected[i] != keystone[64 + i]) return false;
        return true;
    }

    /// <summary>
    /// Confirms the file is a keystone: 96 bytes, correct magic header,
    /// and a valid integrity HMAC. Returns null on success, or a short
    /// description of what's wrong.
    /// </summary>
    public static string? DescribeFile(byte[]? data)
    {
        if (data is null) return "File is empty.";
        if (data.Length != KeystoneLength)
            return $"File is {data.Length} bytes, expected {KeystoneLength}.";

        for (int i = 0; i < 8; i++)
            if (data[i] != Header[i])
                return "File does not start with the 'keystone' magic header.";

        if (!VerifyIntegrity(data))
            return "Integrity HMAC does not match — keystone may be corrupt.";

        return null;
    }

    public static byte[] ComputeFingerprint(string passcode)
    {
        byte[] secret = Convert.FromHexString(PasscodeSecretHex);
        using var hmac = new HMACSHA256(secret);
        return hmac.ComputeHash(Encoding.ASCII.GetBytes(passcode));
    }

    public static void ValidatePasscode(string passcode)
    {
        if (passcode is null)
            throw new ArgumentNullException(nameof(passcode));
        if (passcode.Length != PasscodeLength)
            throw new ArgumentException(
                $"Passcode must be exactly {PasscodeLength} characters long.", nameof(passcode));
        foreach (char c in passcode)
        {
            if (c > 127)
                throw new ArgumentException(
                    "Passcode must contain only ASCII characters.", nameof(passcode));
        }
    }

    public static string GenerateRandomPasscode()
    {
        const string chars =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var sb = new StringBuilder(PasscodeLength);
        byte[] buf = new byte[PasscodeLength];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(buf);
        for (int i = 0; i < PasscodeLength; i++)
            sb.Append(chars[buf[i] % chars.Length]);
        return sb.ToString();
    }

    /// <summary>
    /// Pretty hex dump with section headers. Used by the UI to render
    /// the keystone structure.
    /// </summary>
    public static string FormatHexDump(byte[] data)
    {
        var sb = new StringBuilder();
        if (data.Length >= 32)
        {
            sb.AppendLine("=== Constant header (bytes 0x00-0x1F) ===");
            DumpRange(sb, data, 0, 32);
        }
        if (data.Length >= 64)
        {
            sb.AppendLine();
            sb.AppendLine("=== Passcode fingerprint (bytes 0x20-0x3F) ===");
            DumpRange(sb, data, 32, 32);
        }
        if (data.Length >= 96)
        {
            sb.AppendLine();
            sb.AppendLine("=== Integrity HMAC (bytes 0x40-0x5F) ===");
            DumpRange(sb, data, 64, 32);
        }
        return sb.ToString();
    }

    private static void DumpRange(StringBuilder sb, byte[] data, int offset, int count)
    {
        for (int i = 0; i < count; i += 16)
        {
            sb.Append((offset + i).ToString("X8")).Append("  ");
            for (int j = 0; j < 16 && i + j < count; j++)
                sb.Append(data[offset + i + j].ToString("X2")).Append(' ');
            sb.AppendLine();
        }
    }
}