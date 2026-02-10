using System.Security.Cryptography;
using ActivityStream.Abstractions;
using ActivityStream.Abstractions;

namespace Content.Core;

/// <summary>
/// Minimal ULID generator implementation.
/// ULID = 48-bit timestamp (ms) + 80-bit random = 128 bits, encoded as 26 Crockford Base32 chars.
/// </summary>
public sealed class UlidIdGenerator : IIdGenerator
{
    private static readonly char[] CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();

    // Unix epoch for ULID
    private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public string NewId()
    {
        return NewId(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Generates a ULID for a specific timestamp (useful for testing).
    /// </summary>
    public string NewId(DateTimeOffset timestamp)
    {
        var ms = (ulong)(timestamp - UnixEpoch).TotalMilliseconds;
        Span<byte> randomBytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(randomBytes);

        return Encode(ms, randomBytes);
    }

    private static string Encode(ulong timestampMs, ReadOnlySpan<byte> randomBytes)
    {
        Span<char> result = stackalloc char[26];

        // Encode 48-bit timestamp (6 bytes) as 10 chars
        result[0] = CrockfordBase32[(int)((timestampMs >> 45) & 0x1F)];
        result[1] = CrockfordBase32[(int)((timestampMs >> 40) & 0x1F)];
        result[2] = CrockfordBase32[(int)((timestampMs >> 35) & 0x1F)];
        result[3] = CrockfordBase32[(int)((timestampMs >> 30) & 0x1F)];
        result[4] = CrockfordBase32[(int)((timestampMs >> 25) & 0x1F)];
        result[5] = CrockfordBase32[(int)((timestampMs >> 20) & 0x1F)];
        result[6] = CrockfordBase32[(int)((timestampMs >> 15) & 0x1F)];
        result[7] = CrockfordBase32[(int)((timestampMs >> 10) & 0x1F)];
        result[8] = CrockfordBase32[(int)((timestampMs >> 5) & 0x1F)];
        result[9] = CrockfordBase32[(int)(timestampMs & 0x1F)];

        // Encode 80-bit random (10 bytes) as 16 chars
        ulong high = ((ulong)randomBytes[0] << 32) | ((ulong)randomBytes[1] << 24) |
                     ((ulong)randomBytes[2] << 16) | ((ulong)randomBytes[3] << 8) |
                     randomBytes[4];
        ulong low = ((ulong)randomBytes[5] << 32) | ((ulong)randomBytes[6] << 24) |
                    ((ulong)randomBytes[7] << 16) | ((ulong)randomBytes[8] << 8) |
                    randomBytes[9];

        // Encode high 40 bits (8 chars)
        result[10] = CrockfordBase32[(int)((high >> 35) & 0x1F)];
        result[11] = CrockfordBase32[(int)((high >> 30) & 0x1F)];
        result[12] = CrockfordBase32[(int)((high >> 25) & 0x1F)];
        result[13] = CrockfordBase32[(int)((high >> 20) & 0x1F)];
        result[14] = CrockfordBase32[(int)((high >> 15) & 0x1F)];
        result[15] = CrockfordBase32[(int)((high >> 10) & 0x1F)];
        result[16] = CrockfordBase32[(int)((high >> 5) & 0x1F)];
        result[17] = CrockfordBase32[(int)(high & 0x1F)];

        // Encode low 40 bits (8 chars)
        result[18] = CrockfordBase32[(int)((low >> 35) & 0x1F)];
        result[19] = CrockfordBase32[(int)((low >> 30) & 0x1F)];
        result[20] = CrockfordBase32[(int)((low >> 25) & 0x1F)];
        result[21] = CrockfordBase32[(int)((low >> 20) & 0x1F)];
        result[22] = CrockfordBase32[(int)((low >> 15) & 0x1F)];
        result[23] = CrockfordBase32[(int)((low >> 10) & 0x1F)];
        result[24] = CrockfordBase32[(int)((low >> 5) & 0x1F)];
        result[25] = CrockfordBase32[(int)(low & 0x1F)];

        return new string(result);
    }
}
