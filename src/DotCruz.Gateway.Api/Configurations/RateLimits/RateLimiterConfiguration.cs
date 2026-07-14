using DotCruz.Gateway.Api.Configurations.RateLimits.Options;

namespace DotCruz.Gateway.Api.Configurations.RateLimits;

public static class RateLimiterConfiguration
{
    public static IServiceCollection AddGatewayRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Global, context => GlobalRateLimitPolicy.Build(context));
            options.AddPolicy(RateLimitPolicies.Strict, context => StrictRateLimitPolicy.Build(context));
        });

        return services;
    }
}
