using System.Security.Cryptography;
using System.Text;

namespace YuktiraERP.Infrastructure.Services;

/// <summary>
/// RFC 6238 TOTP implementation (HMAC-SHA1, 6-digit, 30-second window).
/// No external package required.
/// </summary>
public static class MfaTotpService
{
    private const int StepSeconds = 30;
    private const int CodeDigits = 6;

    public static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public static string GenerateCode(string secret, DateTime utcNow)
    {
        var key = Base32Decode(secret);
        var counter = (long)(utcNow.ToUnixTimeSeconds() / StepSeconds);
        var counterBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(counter));
        var hash = new HMACSHA1(key).ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);
        var otp = binary % (int)Math.Pow(10, CodeDigits);
        return otp.ToString($"D{CodeDigits}");
    }

    public static bool VerifyCode(string secret, string code, DateTime utcNow, int toleranceSteps = 1)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;
        var normalized = code.Trim();
        for (var t = -toleranceSteps; t <= toleranceSteps; t++)
        {
            if (GenerateCode(secret, utcNow.AddSeconds(t * StepSeconds)) == normalized)
                return true;
        }
        return false;
    }

    private static long ToUnixTimeSeconds(this DateTime dt)
        => (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder();
        int bitBuffer = 0, bitCount = 0;
        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                sb.Append(Base32Chars[(bitBuffer >> (bitCount - 5)) & 0x1F]);
                bitCount -= 5;
            }
        }
        if (bitCount > 0)
            sb.Append(Base32Chars[(bitBuffer << (5 - bitCount)) & 0x1F]);
        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var clean = input.ToUpperInvariant().TrimEnd('=');
        var list = new List<byte>();
        int bitBuffer = 0, bitCount = 0;
        foreach (var c in clean)
        {
            var value = Base32Chars.IndexOf(c);
            if (value < 0) continue;
            bitBuffer = (bitBuffer << 5) | value;
            bitCount += 5;
            if (bitCount >= 8)
            {
                list.Add((byte)((bitBuffer >> (bitCount - 8)) & 0xFF));
                bitCount -= 8;
            }
        }
        return list.ToArray();
    }
}