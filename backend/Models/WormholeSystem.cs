namespace Sextant.Models;

public class WormholeSystem
{
    public int Id { get; set; }
    public long SolarSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SecondaryName { get; set; }
    public string Class { get; set; } = string.Empty;
    public int? ClassNumber { get; set; }
    public string? Effect { get; set; }
    public string Statics { get; set; } = string.Empty;
    public bool IsShattered { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
