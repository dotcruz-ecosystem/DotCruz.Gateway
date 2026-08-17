using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace DotCruz.Gateway.Api.Configurations.RateLimits.Options;

public static class StrictRateLimitPolicy
{
    public static RateLimitPartition<string> Build(HttpContext context)
    {
        var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value.Strict;

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter($"ip:{clientIp}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = 0
        });
    }
}
