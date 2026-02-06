using Chat.Abstractions;

namespace Chat.Core;

/// <summary>
/// ULID-based ID generator for chat entities.
/// </summary>
public sealed class UlidIdGenerator : IIdGenerator
{
    private static readonly char[] Base32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();
    private static readonly Random Random = new();
    private static readonly object Lock = new();
    private static long _lastTimestamp;
    private static readonly byte[] _lastRandom = new byte[10];

    public string NewId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> randomBytes = stackalloc byte[10];

        lock (Lock)
        {
            if (timestamp == _lastTimestamp)
            {
                // Increment random part for same millisecond
                IncrementRandom();
                _lastRandom.CopyTo(randomBytes);
            }
            else
            {
                // New timestamp, generate fresh random
                Random.NextBytes(randomBytes);
                randomBytes.CopyTo(_lastRandom);
                _lastTimestamp = timestamp;
            }
        }

        return Encode(timestamp, randomBytes);
    }

    private static void IncrementRandom()
    {
        for (int i = 9; i >= 0; i--)
        {
            if (++_lastRandom[i] != 0) break;
        }
    }

    private static string Encode(long timestamp, Span<byte> random)
    {
        Span<char> chars = stackalloc char[26];

        // Encode 48-bit timestamp (10 chars)
        chars[0] = Base32Chars[(int)((timestamp >> 45) & 0x1F)];
        chars[1] = Base32Chars[(int)((timestamp >> 40) & 0x1F)];
        chars[2] = Base32Chars[(int)((timestamp >> 35) & 0x1F)];
        chars[3] = Base32Chars[(int)((timestamp >> 30) & 0x1F)];
        chars[4] = Base32Chars[(int)((timestamp >> 25) & 0x1F)];
        chars[5] = Base32Chars[(int)((timestamp >> 20) & 0x1F)];
        chars[6] = Base32Chars[(int)((timestamp >> 15) & 0x1F)];
        chars[7] = Base32Chars[(int)((timestamp >> 10) & 0x1F)];
        chars[8] = Base32Chars[(int)((timestamp >> 5) & 0x1F)];
        chars[9] = Base32Chars[(int)(timestamp & 0x1F)];

        // Encode 80-bit random (16 chars)
        chars[10] = Base32Chars[(random[0] >> 3) & 0x1F];
        chars[11] = Base32Chars[((random[0] << 2) | (random[1] >> 6)) & 0x1F];
        chars[12] = Base32Chars[(random[1] >> 1) & 0x1F];
        chars[13] = Base32Chars[((random[1] << 4) | (random[2] >> 4)) & 0x1F];
        chars[14] = Base32Chars[((random[2] << 1) | (random[3] >> 7)) & 0x1F];
        chars[15] = Base32Chars[(random[3] >> 2) & 0x1F];
        chars[16] = Base32Chars[((random[3] << 3) | (random[4] >> 5)) & 0x1F];
        chars[17] = Base32Chars[random[4] & 0x1F];
        chars[18] = Base32Chars[(random[5] >> 3) & 0x1F];
        chars[19] = Base32Chars[((random[5] << 2) | (random[6] >> 6)) & 0x1F];
        chars[20] = Base32Chars[(random[6] >> 1) & 0x1F];
        chars[21] = Base32Chars[((random[6] << 4) | (random[7] >> 4)) & 0x1F];
        chars[22] = Base32Chars[((random[7] << 1) | (random[8] >> 7)) & 0x1F];
        chars[23] = Base32Chars[(random[8] >> 2) & 0x1F];
        chars[24] = Base32Chars[((random[8] << 3) | (random[9] >> 5)) & 0x1F];
        chars[25] = Base32Chars[random[9] & 0x1F];

        return new string(chars);
    }
}
