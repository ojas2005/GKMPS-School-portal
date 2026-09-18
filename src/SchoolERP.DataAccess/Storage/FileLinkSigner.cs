using System.Security.Cryptography;
using System.Text;

namespace SchoolERP.DataAccess.Storage;

/// <summary>
/// Short-lived, tamper-proof download links for stored files -- the same idea as an Azure SAS
/// URL. The link carries an expiry and an HMAC over (container, path, expiry), so it can be
/// opened in a new tab without a login token, but can't be altered or reused once expired.
/// </summary>
public class FileLinkSigner
{
    private readonly byte[] _key;

    public FileLinkSigner(IConfiguration configuration)
    {
        var secret = configuration["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("Jwt:SigningKey is required to sign file links.");
        // A key of its own, derived from the JWT secret, so a file-link signature can never
        // double as anything token-related.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes("file-links:" + secret));
    }

    public string Sign(string container, string path, long expiresUnixSeconds)
    {
        var data = Encoding.UTF8.GetBytes($"{container}\n{path}\n{expiresUnixSeconds}");
        return Convert.ToHexString(HMACSHA256.HashData(_key, data)).ToLowerInvariant();
    }

    public bool IsValid(string container, string path, long expiresUnixSeconds, string? signature, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(signature) || now.ToUnixTimeSeconds() > expiresUnixSeconds)
            return false;

        var expected = Encoding.ASCII.GetBytes(Sign(container, path, expiresUnixSeconds));
        var given = Encoding.ASCII.GetBytes(signature.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, given);
    }

    /// <summary>A relative link to the API's file endpoint; the frontend prefixes its API base URL.</summary>
    public string CreateLink(string container, string path, TimeSpan validFor, DateTimeOffset now)
    {
        var expires = now.Add(validFor).ToUnixTimeSeconds();
        var encodedPath = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
        return $"/api/files/{Uri.EscapeDataString(container)}/{encodedPath}?expires={expires}&sig={Sign(container, path, expires)}";
    }
}
