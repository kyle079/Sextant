namespace Sextant.Models;

public class WormholeType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? EveTypeId { get; set; }
    public string? DestinationType { get; set; }
    public int? DestinationClass { get; set; }
    public int LifetimeHours { get; set; }
    public long? MaxMassKg { get; set; }
    public long? MaxJumpMassKg { get; set; }
    public decimal? ScanStrength { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
