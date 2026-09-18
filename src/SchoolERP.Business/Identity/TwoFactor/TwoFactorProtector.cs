using System.Security.Cryptography;
using System.Text;

namespace SchoolERP.Business.Identity.TwoFactor;

/// <summary>
/// Encrypts authenticator secrets at rest (AES-256-GCM), so a copy of the database -- a
/// backup, say -- doesn't hand out everyone's second factor. The key is derived from the
/// JWT signing secret, which lives only in the app's secret store.
/// </summary>
public class TwoFactorProtector
{
    private readonly byte[] _key;

    public TwoFactorProtector(IConfiguration configuration)
    {
        var secret = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required to protect two-step sign-in secrets.");
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
}
