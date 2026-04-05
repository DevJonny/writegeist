using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class LinkedInFetcher : IContentFetcher
{
    public Platform Platform => Platform.LinkedIn;

    public Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        throw new NotSupportedException(
            "Automated fetching is not available for LinkedIn. " +
            "Please use 'From File' or 'Interactive Paste' to import your LinkedIn posts manually.");
    }
}
