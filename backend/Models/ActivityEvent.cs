namespace Sextant.Models;

public enum ActivityEventType
{
    Sig, Chain, Mass, Rolling, PI, Kill, Fit, Structure, Site, Auth, System
}

public class ActivityEvent
{
    public int Id { get; set; }
    public ActivityEventType Type { get; set; }
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? DetailJson { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long? SystemId { get; set; }
}
