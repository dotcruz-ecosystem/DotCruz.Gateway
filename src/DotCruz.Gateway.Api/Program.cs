using DotCruz.Gateway.Api.Configurations.Cors;
using DotCruz.Gateway.Api.Configurations.RateLimits;
using DotCruz.Gateway.Api.Configurations.Resilience;
using DotCruz.Gateway.Api.Configurations.ReverseProxy;
using DotCruz.Gateway.Api.Configurations.Tracing;
using DotCruz.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedSecurity(builder.Configuration);

builder.Services.AddGatewayReverseProxy(builder.Configuration);

builder.Services.AddGatewayCorsConfiguration(builder.Configuration);

builder.Services.AddGatewayRateLimiter();

builder.Services.AddGatewayTracing(builder.Environment);
builder.Services.AddGatewayResilience();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
