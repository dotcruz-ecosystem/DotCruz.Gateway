using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace DotCruz.Gateway.Api.Configurations.RateLimits.Options;

public static class GlobalRateLimitPolicy
{
    public static RateLimitPartition<string> Build(HttpContext context)
    {
        var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value.Global;
        var window = TimeSpan.FromSeconds(settings.WindowSeconds);

        var user = context.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst(ClaimTypes.Sid)?.Value
                         ?? "authenticated";

            return RateLimitPartition.GetFixedWindowLimiter($"user:{userId}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.AuthenticatedPermitLimit,
                Window = window,
                QueueLimit = 0
            });
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter($"ip:{clientIp}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.AnonymousPermitLimit,
            Window = window,
            QueueLimit = 0
        });
    }
}
