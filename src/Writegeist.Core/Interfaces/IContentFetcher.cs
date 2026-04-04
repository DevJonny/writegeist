using Writegeist.Core.Models;

namespace Writegeist.Core.Interfaces;

public interface IContentFetcher
{
    Platform Platform { get; }
    Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request);
}
