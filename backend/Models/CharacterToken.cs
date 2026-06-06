namespace Sextant.Models;

public class CharacterToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int CorporationId { get; set; }
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
