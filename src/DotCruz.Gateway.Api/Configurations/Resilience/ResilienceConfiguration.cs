namespace DotCruz.Gateway.Api.Configurations.Resilience;

public static class ResilienceConfiguration
{
    public static IServiceCollection AddGatewayResilience(this IServiceCollection services)
    {
        services.AddHttpClient("Microsoft.ReverseProxy")
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 8;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });

        return services;
    }
}
