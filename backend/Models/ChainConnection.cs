namespace Sextant.Models;

public class ChainConnection
{
    public int Id { get; set; }
    public int SourceSystemId { get; set; }
    public ChainSystem SourceSystem { get; set; } = null!;
    public int TargetSystemId { get; set; }
    public ChainSystem TargetSystem { get; set; } = null!;
    public string WormholeTypeCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Stable";
    public string MassStatus { get; set; } = "Fresh";
    public long? MaxMassKg { get; set; }
    public long? MaxJumpMassKg { get; set; }
    public int? LifetimeHours { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EolAt { get; set; }
    public string? SignatureId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
