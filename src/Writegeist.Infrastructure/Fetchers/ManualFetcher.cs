using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class ManualFetcher : IContentFetcher
{
    public Platform Platform { get; }

    public ManualFetcher(Platform platform)
    {
        Platform = platform;
    }

    public async Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            throw new ArgumentException("FilePath is required for file import.", nameof(request));

        var content = await File.ReadAllTextAsync(request.FilePath);
        var posts = content
            .Split(["---"], StringSplitOptions.None)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => new FetchedPost(p))
            .ToList();

        return posts;
    }
}
