namespace Sextant.Models;

public enum UserRole { Admin, Member, ReadOnly }

public class User
{
    public int Id { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public long PrimaryCharacterId { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    public ICollection<CharacterToken> Characters { get; set; } = [];
}
