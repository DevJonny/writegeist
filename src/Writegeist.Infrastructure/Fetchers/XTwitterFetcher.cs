using System.Net.Http.Headers;
using System.Text.Json;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Fetchers;

public class XTwitterFetcher(IHttpClientFactory httpClientFactory, ISecretProvider secrets) : IContentFetcher
{
    private const string BaseUrl = "https://api.x.com/2";
    private const int MaxResults = 100;

    public Platform Platform => Platform.X;

    public async Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(FetchRequest request)
    {
        var bearerToken = secrets.Require("X_BEARER_TOKEN", "X/Twitter Bearer Token");

        var input = request.Handle ?? request.Url
            ?? throw new ArgumentException("A handle or URL is required to fetch tweets.", nameof(request));

        var handle = input.TrimStart('@');
        if (Uri.TryCreate(handle, UriKind.Absolute, out var uri))
            handle = uri.AbsolutePath.Trim('/').Split('/')[0];

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var userId = await ResolveUserIdAsync(httpClient, handle);
        return await FetchTweetsAsync(httpClient, userId);
    }

    private static async Task<string> ResolveUserIdAsync(HttpClient httpClient, string handle)
    {
        var response = await httpClient.GetAsync($"{BaseUrl}/users/by/username/{handle}");

        if ((int)response.StatusCode == 429)
            throw new HttpRequestException(
                "X API rate limit exceeded. Please wait a few minutes and try again.");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("errors", out var errors))
        {
            var message = errors[0].GetProperty("detail").GetString();
            throw new HttpRequestException($"X API error: {message}");
        }

        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()
            ?? throw new HttpRequestException("Could not resolve X user ID.");
    }

    private static async Task<IReadOnlyList<FetchedPost>> FetchTweetsAsync(HttpClient httpClient, string userId)
    {
        var url = $"{BaseUrl}/users/{userId}/tweets?max_results={MaxResults}&tweet.fields=created_at";
        var response = await httpClient.GetAsync(url);

        if ((int)response.StatusCode == 429)
            throw new HttpRequestException(
                "X API rate limit exceeded. Please wait a few minutes and try again.");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("data", out var data))
            return [];

        var posts = new List<FetchedPost>();
        foreach (var tweet in data.EnumerateArray())
        {
            var content = tweet.GetProperty("text").GetString() ?? string.Empty;
            var tweetId = tweet.GetProperty("id").GetString();
            var sourceUrl = tweetId is not null ? $"https://x.com/i/status/{tweetId}" : null;

            DateTime? publishedAt = null;
            if (tweet.TryGetProperty("created_at", out var createdAt))
                publishedAt = DateTime.Parse(createdAt.GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

            posts.Add(new FetchedPost(content, sourceUrl, publishedAt));
        }

        return posts;
    }
}
