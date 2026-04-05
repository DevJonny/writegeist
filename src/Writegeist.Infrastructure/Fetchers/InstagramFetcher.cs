using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class InstagramFetcher : IContentFetcher
{
    public Platform Platform => Platform.Instagram;

    public Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        throw new NotSupportedException(
            "Automated fetching is not available for Instagram. " +
            "Please use 'From File' or 'Interactive Paste' to import your Instagram posts manually.");
    }
}
