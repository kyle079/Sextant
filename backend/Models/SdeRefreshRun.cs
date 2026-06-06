namespace Sextant.Models;

public enum SdeRefreshState
{
    Running, Succeeded, Failed
}

public class SdeRefreshRun
{
    public int Id { get; set; }
    public SdeRefreshState State { get; set; } = SdeRefreshState.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int WormholeSystemCount { get; set; }
    public int WormholeTypeCount { get; set; }
    public string? Error { get; set; }
}
