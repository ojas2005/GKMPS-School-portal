using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SchoolERP.Common.Logging;

/// <summary>
/// Serilog setup: console output (for `docker compose logs` / the hosting platform's log
/// stream), plus the same structured events to Seq when <c>Seq:Url</c> is configured -- a
/// bare `dotnet run` without it simply skips Seq, no crash, no dangling connection attempt.
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
