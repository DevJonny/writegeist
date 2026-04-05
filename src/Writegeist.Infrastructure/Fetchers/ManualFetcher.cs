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

        var lines = await File.ReadAllLinesAsync(request.FilePath);
        var posts = new List<FetchedPost>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (current.Count > 0)
                {
                    posts.Add(new FetchedPost(string.Join(Environment.NewLine, current).Trim()));
                    current.Clear();
                }
            }
            else
            {
                current.Add(line);
            }
        }

        if (current.Count > 0)
            posts.Add(new FetchedPost(string.Join(Environment.NewLine, current).Trim()));

        posts.RemoveAll(p => string.IsNullOrEmpty(p.Content));

        return posts;
    }
}
