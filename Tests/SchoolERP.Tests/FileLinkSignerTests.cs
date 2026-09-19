using Microsoft.Extensions.Configuration;
using SchoolERP.DataAccess.Storage;

namespace SchoolERP.Tests;

public class FileLinkSignerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 10, 0, 0, TimeSpan.Zero);

    private static FileLinkSigner Signer(string key = "0123456789abcdef0123456789abcdef") =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key }).Build());

    private static (long Expires, string Sig) Parse(string link)
    {
        var query = link[(link.IndexOf('?') + 1)..].Split('&').Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
        return (long.Parse(query["expires"]), query["sig"]);
    }

    [Fact]
    public void A_fresh_link_opens_its_own_file()
    {
        var signer = Signer();
        var link = signer.CreateLink("fee-receipts", "student-1/tx-1.pdf", TimeSpan.FromMinutes(15), Now);
        var (expires, sig) = Parse(link);

        Assert.StartsWith("/api/files/fee-receipts/student-1/tx-1.pdf?", link);
        Assert.True(signer.IsValid("fee-receipts", "student-1/tx-1.pdf", expires, sig, Now.AddMinutes(14)));
    }

    [Fact]
    public void An_expired_link_is_refused()
    {
        var signer = Signer();
        var (expires, sig) = Parse(signer.CreateLink("fee-receipts", "s/t.pdf", TimeSpan.FromMinutes(15), Now));

        Assert.False(signer.IsValid("fee-receipts", "s/t.pdf", expires, sig, Now.AddMinutes(16)));
    }

    [Fact]
    public void A_link_cannot_be_pointed_at_someone_elses_file()
    {
        var signer = Signer();
        var (expires, sig) = Parse(signer.CreateLink("fee-receipts", "student-1/tx-1.pdf", TimeSpan.FromMinutes(15), Now));

        Assert.False(signer.IsValid("fee-receipts", "student-2/tx-9.pdf", expires, sig, Now));
        Assert.False(signer.IsValid("report-cards", "student-1/tx-1.pdf", expires, sig, Now));
    }

    [Fact]
    public void Stretching_the_expiry_breaks_the_signature()
    {
        var signer = Signer();
        var (expires, sig) = Parse(signer.CreateLink("fee-receipts", "s/t.pdf", TimeSpan.FromMinutes(15), Now));

        Assert.False(signer.IsValid("fee-receipts", "s/t.pdf", expires + 86_400, sig, Now));
    }

    [Fact]
    public void Links_signed_with_a_different_secret_are_refused()
    {
        var (expires, sig) = Parse(Signer("another-secret-another-secret-12345").CreateLink("fee-receipts", "s/t.pdf", TimeSpan.FromMinutes(15), Now));

        Assert.False(Signer().IsValid("fee-receipts", "s/t.pdf", expires, sig, Now));
    }

    [Fact]
    public void A_missing_signature_is_refused()
    {
        Assert.False(Signer().IsValid("fee-receipts", "s/t.pdf", Now.AddMinutes(5).ToUnixTimeSeconds(), null, Now));
    }

    [Fact]
    public void A_link_belongs_to_the_session_it_was_made_for()
    {
        var signer = Signer();
        var mine = Guid.NewGuid();
        var link = signer.CreateLink("fee-receipts", "s/t.pdf", TimeSpan.FromMinutes(15), Now, mine);
        var (expires, sig) = Parse(link);

        Assert.Contains($"&s={mine}", link);
        Assert.True(signer.IsValid("fee-receipts", "s/t.pdf", expires, sig, Now, mine));
        Assert.False(signer.IsValid("fee-receipts", "s/t.pdf", expires, sig, Now, Guid.NewGuid()));
        Assert.False(signer.IsValid("fee-receipts", "s/t.pdf", expires, sig, Now, null));   // stripping the session doesn't help
    }
}
