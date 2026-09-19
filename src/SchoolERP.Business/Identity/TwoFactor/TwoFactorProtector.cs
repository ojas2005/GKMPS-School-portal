using System.Security.Cryptography;
using System.Text;

namespace SchoolERP.Business.Identity.TwoFactor;

/// <summary>
/// Encrypts authenticator secrets at rest (AES-256-GCM), so a copy of the database -- a
/// backup, say -- doesn't hand out everyone's second factor. The key comes from
/// TwoFactor:EncryptionKey, a secret of its own, so the JWT signing key can be rotated after an
/// incident without breaking everyone's authenticator. Without it, the JWT signing key is used.
/// </summary>
public class TwoFactorProtector
{
    private readonly byte[] _key;

    public TwoFactorProtector(IConfiguration configuration)
    {
        var secret = configuration["TwoFactor:EncryptionKey"] is { Length: > 0 } own ? own
            : configuration["Jwt:SigningKey"]
              ?? throw new InvalidOperationException("TwoFactor:EncryptionKey or Jwt:SigningKey is required to protect two-step sign-in secrets.");
        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret), 32, info: Encoding.UTF8.GetBytes("gkmps/two-factor-secrets"));
    }

    public string Protect(byte[] secret)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[secret.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_key, tag.Length)) aes.Encrypt(nonce, secret, cipher, tag);
        return "v1:" + Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public byte[] Unprotect(string protectedSecret)
    {
        if (!protectedSecret.StartsWith("v1:")) throw new CryptographicException("Unknown secret format.");
        var raw = Convert.FromBase64String(protectedSecret[3..]);
        var nonce = raw[..12]; var tag = raw[12..28]; var cipher = raw[28..];
        var plain = new byte[cipher.Length];
        using (var aes = new AesGcm(_key, tag.Length)) aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    /// <summary>
    /// The secret, or null when it can't be read -- it was encrypted under a key that has since
    /// changed. Callers treat that as a wrong code, so recovery codes (which don't depend on the
    /// key) still sign the user in.
    /// </summary>
    public byte[]? TryUnprotect(string protectedSecret)
    {
        try { return Unprotect(protectedSecret); }
        catch (Exception e) when (e is CryptographicException or FormatException or ArgumentException) { return null; }
    }
}
