using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace DotCruz.Gateway.Api.Configurations.ForwardedHeaders;

public static class ForwardedHeadersConfiguration
{
    private const string DefaultDockerNetwork = "172.16.0.0/12";

    public static IServiceCollection AddGatewayForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var knownNetworks = configuration.GetSection("Settings:ForwardedHeaders:KnownNetworks").Get<string[]>()
                            ?? [DefaultDockerNetwork];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

            options.ForwardLimit = 1;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var network in knownNetworks)
            {
                var parts = network.Split('/');

                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
                }
                else if (IPAddress.TryParse(network, out var proxy))
                {
                    options.KnownProxies.Add(proxy);
                }
            }
        });

        return services;
    }
}
