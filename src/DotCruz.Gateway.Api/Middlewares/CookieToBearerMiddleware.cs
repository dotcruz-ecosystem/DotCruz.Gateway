using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
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

        if (requestPath != null && requestPath.EndsWith("/auth/refresh-token", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
            {
                var payload = JsonSerializer.Serialize(new { refresh_token = refreshToken });
                var bytes = Encoding.UTF8.GetBytes(payload);
                context.Request.Body = new MemoryStream(bytes);
                context.Request.ContentType = "application/json";
                context.Request.ContentLength = bytes.Length;
                context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>(new RefreshTokenBodyDetectionFeature());
            }
        }

        var isLogoutRoute = requestPath != null && requestPath.EndsWith("/auth/logout");
        if (isLogoutRoute)
        {
            var isHttps = context.Request.IsHttps || 
                          string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

            context.Response.Cookies.Delete("access_token", new CookieOptions { Path = "/", Secure = isHttps, SameSite = SameSiteMode.Lax });
            context.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/", Secure = isHttps, SameSite = SameSiteMode.Lax });
        }

        await next(context);
    }

    private sealed class RefreshTokenBodyDetectionFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
