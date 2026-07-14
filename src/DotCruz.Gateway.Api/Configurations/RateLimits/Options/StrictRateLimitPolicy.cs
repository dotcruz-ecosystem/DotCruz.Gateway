using System.Threading.RateLimiting;

namespace DotCruz.Gateway.Api.Configurations.RateLimits.Options;

public static class StrictRateLimitPolicy
{
    public static RateLimitPartition<string> Build(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            QueueLimit = 0
        });
    }
}
