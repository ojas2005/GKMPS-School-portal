using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SchoolERP.Shared.Logging;

/// <summary>
/// One Serilog setup shared by every service, so "add monitoring" was a one-file change
/// instead of touching 13 Program.cs files' logging config individually. Every service
/// keeps its existing console output (for `docker compose logs`) and additionally ships
/// the same structured events to Seq when <c>Seq:Url</c> is configured -- which the
/// gateway/compose environment sets, but a bare `dotnet run` outside Docker simply skips,
/// no crash, no dangling connection attempt.
/// </summary>
public static class SharedLogging
{
    public static Action<HostBuilderContext, IServiceProvider, LoggerConfiguration> Configure(string serviceName) =>
        (context, _, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console();

            var seqUrl = context.Configuration["Seq:Url"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
                configuration.WriteTo.Seq(seqUrl);
        };
}
