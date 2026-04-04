namespace Writegeist.Core.Models;

public class RawPost
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Platform Platform { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public DateTime FetchedAt { get; set; }
}
