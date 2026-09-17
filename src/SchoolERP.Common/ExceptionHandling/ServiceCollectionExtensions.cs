using Microsoft.Extensions.DependencyInjection;

namespace SchoolERP.Common.ExceptionHandling;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="GlobalExceptionHandler"/> plus the ProblemDetails
    /// service it needs. Pair with <c>app.UseExceptionHandler()</c> early in the pipeline
    /// (right after <c>var app = builder.Build()</c>) in Program.cs.</summary>
    public static IServiceCollection AddSharedExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
