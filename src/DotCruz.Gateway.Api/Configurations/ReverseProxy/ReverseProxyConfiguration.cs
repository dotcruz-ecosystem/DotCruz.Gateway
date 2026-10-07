using DotCruz.Shared.Security.CustomClaim;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

namespace DotCruz.Gateway.Api.Configurations.ReverseProxy;

public static class ReverseProxyConfiguration
{
    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthCookieSettings>(configuration.GetSection("Settings:AuthCookies"));

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestHeaderRemove("X-Api-Key");
                builderContext.AddRequestHeaderRemove("X-Service-Name");
                builderContext.AddRequestHeaderRemove("X-Workspace-ID");

                builderContext.AddRequestTransform(transformContext =>
                {
                    var user = transformContext.HttpContext.User;
                    var workspaceIdClaim = user.FindFirst(CustomClaimsTypes.WorkspaceId)?.Value
                                           ?? user.FindFirst("workspace_id")?.Value;

                    if (!string.IsNullOrEmpty(workspaceIdClaim))
                    {
                        transformContext.ProxyRequest.Headers.Remove("X-Workspace-ID");
                        transformContext.ProxyRequest.Headers.Add("X-Workspace-ID", workspaceIdClaim);
                    }

                    return ValueTask.CompletedTask;
                });

                builderContext.AddResponseTransform(async transformContext =>
                {
                    var httpContext = transformContext.HttpContext;
                    var requestPath = httpContext.Request.Path.Value;

                    var isAuthRoute = requestPath != null && 
                                      (requestPath.EndsWith("/auth/login") || requestPath.EndsWith("/auth/refresh-token"));

                    if (isAuthRoute && transformContext.ProxyResponse != null && transformContext.ProxyResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var responseContent = transformContext.ProxyResponse.Content;
                        if (responseContent != null)
                        {
                            var responseBody = await responseContent.ReadAsStringAsync();
                            
                            transformContext.ProxyResponse.Content = new StringContent(
                                responseBody, 
                                System.Text.Encoding.UTF8, 
                                responseContent.Headers.ContentType?.MediaType ?? "application/json"
                            );

                            var cookieSettings = httpContext.RequestServices
                                .GetRequiredService<IOptions<AuthCookieSettings>>().Value;

                            try
                            {
                                using var jsonDoc = JsonDocument.Parse(responseBody);
                                var root = jsonDoc.RootElement;
                                string? newAccessToken = null;
                                string? newRefreshToken = null;

                                static string? GetStringProperty(JsonElement element, params string[] names)
                                {
                                    foreach (var name in names)
                                    {
                                        if (element.TryGetProperty(name, out var prop))
                                            return prop.GetString();
                                    }
                                    return null;
                                }

                                if (root.TryGetProperty("tokens", out var tokensProp))
                                {
                                    newAccessToken = GetStringProperty(tokensProp, "accessToken", "access_token", "AccessToken");
                                    newRefreshToken = GetStringProperty(tokensProp, "refreshToken", "refresh_token", "RefreshToken");
                                }
                                else
                                {
                                    newAccessToken = GetStringProperty(root, "accessToken", "access_token", "AccessToken");
                                    newRefreshToken = GetStringProperty(root, "refreshToken", "refresh_token", "RefreshToken");
                                }

                                var isHttps = httpContext.Request.IsHttps || 
                                              string.Equals(httpContext.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

                                if (!string.IsNullOrEmpty(newAccessToken))
                                {
                                    httpContext.Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = isHttps,
                                        SameSite = SameSiteMode.Lax,
                                        Path = "/",
                                        Expires = DateTimeOffset.UtcNow.AddMinutes(cookieSettings.AccessTokenLifetimeMinutes)
                                    });
                                }

                                if (!string.IsNullOrEmpty(newRefreshToken))
                                {
                                    httpContext.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = isHttps,
                                        SameSite = SameSiteMode.Lax,
                                        Path = "/",
                                        Expires = DateTimeOffset.UtcNow.AddDays(cookieSettings.RefreshTokenLifetimeDays)
                                    });
                                }
                            }
                            catch (JsonException exception)
                            {
                                httpContext.RequestServices
                                    .GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("Gateway.AuthCookies")
                                    .LogError(exception, "Falha ao interpretar a resposta de {Path} para gravar os cookies de sessao.", requestPath);
                            }
                        }
                    }
                });
            });

        return services;
    }
}
