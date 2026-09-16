using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolERP.Shared.Hosting;

namespace SchoolERP.Shared.Tests;

public class SharedHostingTests
{
    [Fact]
    public void Signed_in_requests_are_bucketed_per_user()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "Bearer"));

        Assert.Equal("user:user-1", SharedHosting.ClientPartitionKey(context));
    }

    [Fact]
    public void Anonymous_requests_are_bucketed_per_client_ip()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

        Assert.Equal("ip:203.0.113.9", SharedHosting.ClientPartitionKey(context));
    }

    [Fact]
    public void Two_users_behind_the_same_ip_get_separate_buckets()
    {
        HttpContext For(string userId)
        {
            var c = new DefaultHttpContext();
            c.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.1");
            c.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Bearer"));
            return c;
        }

        Assert.NotEqual(SharedHosting.ClientPartitionKey(For("a")), SharedHosting.ClientPartitionKey(For("b")));
    }

    [Fact]
    public void Migration_is_retried_until_it_succeeds()
    {
        var attempts = 0;
        SharedHosting.MigrateWithRetry(() => { if (++attempts < 2) throw new InvalidOperationException("db not ready"); },
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, maxAttempts: 3);

        Assert.Equal(2, attempts);
    }
}
