using System.Security.Claims;
using System.Threading.RateLimiting;

namespace DotCruz.Gateway.Api.Configurations.RateLimits.Options;

public static class GlobalRateLimitPolicy
{
    public static RateLimitPartition<string> Build(HttpContext context)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? "authenticated";

            return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    }
}
