namespace DomsUtils.Services.Caching.Interfaces.Addons;

public class ICacheHealth
{
    public bool IsHealthy { get; set; }
    public string? HealthMessage { get; set; }
    public HealthState HealthState { get; set; }
}

public enum HealthState
{
    Unknown,
    Healthy,
    Warning,
    Critical,
    Offline
}