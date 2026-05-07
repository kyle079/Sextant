namespace Sextant.Models;

public class ChainSystem
{
    public int Id { get; set; }
    public long SolarSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int? ClassNumber { get; set; }
    public string? Effect { get; set; }
    public string Statics { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsHome { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<ChainConnection> SourceConnections { get; set; } = [];
    public ICollection<ChainConnection> TargetConnections { get; set; } = [];
    public ICollection<Signature> Signatures { get; set; } = [];
}
