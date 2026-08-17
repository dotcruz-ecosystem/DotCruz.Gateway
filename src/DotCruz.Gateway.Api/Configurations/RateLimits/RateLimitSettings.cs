namespace DotCruz.Gateway.Api.Configurations.RateLimits;

public class RateLimitSettings
{
    public GlobalRateLimitSettings Global { get; set; } = new();
    public StrictRateLimitSettings Strict { get; set; } = new();
}

public class GlobalRateLimitSettings
{
    public int AuthenticatedPermitLimit { get; set; } = 100;
    public int AnonymousPermitLimit { get; set; } = 30;
    public int WindowSeconds { get; set; } = 60;
}

public class StrictRateLimitSettings
{
    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 10;
}
