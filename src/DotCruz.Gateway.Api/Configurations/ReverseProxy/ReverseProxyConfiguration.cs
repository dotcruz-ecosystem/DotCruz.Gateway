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
            });

        return services;
    }
}
