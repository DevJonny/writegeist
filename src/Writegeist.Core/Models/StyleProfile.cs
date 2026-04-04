namespace Writegeist.Core.Models;

public class StyleProfile
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string ProfileJson { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
