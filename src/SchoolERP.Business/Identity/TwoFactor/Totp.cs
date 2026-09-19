using System.Security.Cryptography;
using System.Text;

namespace SchoolERP.Business.Identity.TwoFactor;

/// <summary>
/// Time-based one-time passwords (RFC 6238) -- the six-digit codes shown by Google
/// Authenticator, Microsoft Authenticator, Authy and similar apps. HMAC-SHA1, 30-second
/// steps, which is what every such app expects.
/// </summary>
public static class Totp
{
    public const int Digits = 6;
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

    public static long StepAt(DateTimeOffset time) => time.ToUnixTimeSeconds() / (long)Step.TotalSeconds;

    public static string Code(byte[] secret, long step, int digits = Digits)
    {
        Span<byte> counter = stackalloc byte[8];
        for (var i = 7; i >= 0; i--) { counter[i] = (byte)(step & 0xFF); step >>= 8; }
        var hash = HMACSHA1.HashData(secret, counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        var modulo = (int)Math.Pow(10, digits);
        return (binary % modulo).ToString(new string('0', digits));
    }

    /// <summary>
    /// The time-step the code belongs to, allowing one step either side for a phone clock
    /// that's slightly off -- or null if it matches none.
    /// </summary>
    public static long? Match(byte[] secret, string code, DateTimeOffset now)
    {
        code = new string(code.Where(char.IsDigit).ToArray());
        if (code.Length != Digits) return null;
        var current = StepAt(now);
        for (var step = current - 1; step <= current + 1; step++)
        {
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(secret, step)), Encoding.ASCII.GetBytes(code)))
                return step;
        }
        return null;
    }

    /// <summary>The link an authenticator app reads from the QR code.</summary>
    public static string SetupUri(byte[] secret, string account, string issuer) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}" +
        $"?secret={Base32.Encode(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={(int)Step.TotalSeconds}";
}

public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        var output = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b; bits += 8;
            while (bits >= 5) { output.Append(Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
        }
        if (bits > 0) output.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }

    public static byte[] Decode(string text)
    {
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in text.ToUpperInvariant().Where(c => c != '=' && !char.IsWhiteSpace(c)))
        {
            var value = Alphabet.IndexOf(c);
            if (value < 0) throw new FormatException("Not a base32 string.");
            buffer = (buffer << 5) | value; bits += 5;
            if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF)); bits -= 8; }
        }
        return bytes.ToArray();
    }
}
