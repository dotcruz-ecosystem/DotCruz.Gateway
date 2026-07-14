using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DotCruz.Gateway.Api.Configurations.Tracing;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddGatewayTracing(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("DotCruz.Gateway"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext => 
                        {
                            var path = httpContext.Request.Path.Value;
                            return path != null && !path.Contains("/health", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation();

                if (environment.IsDevelopment())
                    tracing.AddConsoleExporter();

                tracing.AddOtlpExporter();
            });

        return services;
    }
}
