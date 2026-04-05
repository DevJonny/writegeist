using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class FacebookFetcher : IContentFetcher
{
    public Platform Platform => Platform.Facebook;

    public Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        throw new NotSupportedException(
            "Automated fetching is not available for Facebook. " +
            "Please use 'From File' or 'Interactive Paste' to import your Facebook posts manually.");
    }
}
