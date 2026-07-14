namespace DotCruz.Gateway.Api.Configurations.Cors;

public static class CorsConfiguration
{
    public static IServiceCollection AddGatewayCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Settings:Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
            });
        });

        return services;
    }
}
