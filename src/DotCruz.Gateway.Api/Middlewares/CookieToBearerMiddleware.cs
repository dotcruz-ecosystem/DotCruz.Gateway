using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DotCruz.Gateway.Api.Middlewares;

public class CookieToBearerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;

        if (context.Request.Cookies.TryGetValue("access_token", out var accessToken))
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Request.Headers.Authorization = $"Bearer {accessToken}";
            }
        }

        var isLogoutRoute = requestPath != null && requestPath.EndsWith("/auth/logout");
        if (isLogoutRoute)
        {
            context.Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
            context.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });
        }

        await next(context);
    }
}
