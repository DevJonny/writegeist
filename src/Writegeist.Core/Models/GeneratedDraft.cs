namespace Writegeist.Core.Models;

public class GeneratedDraft
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int StyleProfileId { get; set; }
    public Platform Platform { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? ParentDraftId { get; set; }
    public string? Feedback { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
