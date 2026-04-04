namespace Writegeist.Core.Models;

public record FetchedPost(string Content, string? SourceUrl = null, DateTime? PublishedAt = null);
