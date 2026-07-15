using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

namespace DotCruz.Gateway.Api.Configurations.ReverseProxy;

public static class ReverseProxyConfiguration
{
    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestHeaderRemove("X-Api-Key");
                builderContext.AddRequestHeaderRemove("X-Service-Name");
                builderContext.AddRequestHeaderRemove("X-Tenant-ID");

                builderContext.AddRequestTransform(transformContext =>
                {
                    var user = transformContext.HttpContext.User;
                    var tenantIdClaim = user.FindFirst("tenant_id")?.Value
                                        ?? user.FindFirst("TenantId")?.Value;

                    if (!string.IsNullOrEmpty(tenantIdClaim))
                    {
                        transformContext.ProxyRequest.Headers.Remove("X-Tenant-ID");
                        transformContext.ProxyRequest.Headers.Add("X-Tenant-ID", tenantIdClaim);
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

                                if (!string.IsNullOrEmpty(newAccessToken))
                                {
                                    httpContext.Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Lax,
                                        Path = "/",
                                        Expires = DateTimeOffset.UtcNow.AddDays(1)
                                    });
                                }

                                if (!string.IsNullOrEmpty(newRefreshToken))
                                {
                                    httpContext.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = false,
                                        SameSite = SameSiteMode.Lax,
                                        Path = "/",
                                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                                    });
                                }
                            }
                            catch { }
                        }
                    }
                });
            });

        return services;
    }
}
