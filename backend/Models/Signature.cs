namespace Sextant.Models;

public class Signature
{
    public int Id { get; set; }
    public int ChainSystemId { get; set; }
    public ChainSystem ChainSystem { get; set; } = null!;
    public string SignatureId { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Type { get; set; } = "Unknown";
    public string? Name { get; set; }
    public decimal ScanPercentage { get; set; }
    public long ScannedByCharacterId { get; set; }
    public string ScannedByCharacterName { get; set; } = string.Empty;
    public DateTime? EolAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
