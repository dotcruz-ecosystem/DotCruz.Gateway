namespace DotCruz.Gateway.Api.Configurations.ReverseProxy;

public class AuthCookieSettings
{
    public int AccessTokenLifetimeMinutes { get; set; } = 120;
    public int RefreshTokenLifetimeDays { get; set; } = 15;
}
