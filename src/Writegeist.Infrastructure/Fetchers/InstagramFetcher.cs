using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class InstagramFetcher(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IContentFetcher
{
    private const string BaseUrl = "https://graph.instagram.com/v25.0";
    private const string MediaFields = "id,caption,media_type,timestamp,permalink";
    private const int PageLimit = 100;

    public Platform Platform => Platform.Instagram;

    public async Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        var accessToken = configuration["INSTAGRAM_ACCESS_TOKEN"]
            ?? throw new InvalidOperationException(
                "Instagram Access Token is not configured. " +
                "Set the INSTAGRAM_ACCESS_TOKEN environment variable, or use 'From File' or 'Interactive Paste' to import posts manually.");

        var httpClient = httpClientFactory.CreateClient();
        return await FetchMediaAsync(httpClient, accessToken);
    }

    private static async Task<IReadOnlyList<FetchedPost>> FetchMediaAsync(HttpClient httpClient, string accessToken)
    {
        var posts = new List<FetchedPost>();
        var url = $"{BaseUrl}/me/media?fields={MediaFields}&limit={PageLimit}&access_token={accessToken}";

        while (url is not null)
        {
            var response = await httpClient.GetAsync(url);

            if ((int)response.StatusCode == 429)
                throw new HttpRequestException(
                    "Instagram API rate limit exceeded. Please wait a few minutes and try again.");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var media in data.EnumerateArray())
                {
                    var caption = media.TryGetProperty("caption", out var cap)
                        ? cap.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(caption))
                        continue;

                    var permalink = media.TryGetProperty("permalink", out var link)
                        ? link.GetString()
                        : null;

                    DateTime? publishedAt = null;
                    if (media.TryGetProperty("timestamp", out var ts))
                        publishedAt = DateTime.Parse(
                            ts.GetString()!,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

                    posts.Add(new FetchedPost(caption, permalink, publishedAt));
                }
            }

            // Follow cursor pagination
            url = null;
            if (doc.RootElement.TryGetProperty("paging", out var paging) &&
                paging.TryGetProperty("next", out var next))
            {
                url = next.GetString();
            }
        }

        return posts;
    }
}
