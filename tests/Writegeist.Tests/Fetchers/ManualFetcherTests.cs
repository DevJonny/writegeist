using FluentAssertions;
using Writegeist.Core.Models;
using Writegeist.Infrastructure.Fetchers;

namespace Writegeist.Tests.Fetchers;

public class ManualFetcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ManualFetcher _fetcher;

    public ManualFetcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"writegeist_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _fetcher = new ManualFetcher(Platform.LinkedIn);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string WriteFile(string content)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task FetchPostsAsync_MultiplePosts_SplitsOnSeparator()
    {
        var path = WriteFile("First post\n---\nSecond post\n---\nThird post");
        var request = new FetchRequest(FilePath: path);

        var posts = await _fetcher.FetchPostsAsync(request);

        posts.Should().HaveCount(3);
        posts[0].Content.Should().Be("First post");
        posts[1].Content.Should().Be("Second post");
        posts[2].Content.Should().Be("Third post");
    }

    [Fact]
    public async Task FetchPostsAsync_EmptyFile_ReturnsEmpty()
    {
        var path = WriteFile("");
        var request = new FetchRequest(FilePath: path);

        var posts = await _fetcher.FetchPostsAsync(request);

        posts.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchPostsAsync_NoSeparators_ReturnsSinglePost()
    {
        var path = WriteFile("Just one post with no separators");
        var request = new FetchRequest(FilePath: path);

        var posts = await _fetcher.FetchPostsAsync(request);

        posts.Should().HaveCount(1);
        posts[0].Content.Should().Be("Just one post with no separators");
    }

    [Fact]
    public async Task FetchPostsAsync_ConsecutiveSeparators_SkipsEmpty()
    {
        var path = WriteFile("First\n---\n---\n---\nSecond");
        var request = new FetchRequest(FilePath: path);

        var posts = await _fetcher.FetchPostsAsync(request);

        posts.Should().HaveCount(2);
        posts[0].Content.Should().Be("First");
        posts[1].Content.Should().Be("Second");
    }

    [Fact]
    public async Task FetchPostsAsync_TrimsWhitespace()
    {
        var path = WriteFile("  First post  \n---\n  Second post  ");
        var request = new FetchRequest(FilePath: path);

        var posts = await _fetcher.FetchPostsAsync(request);

        posts[0].Content.Should().Be("First post");
        posts[1].Content.Should().Be("Second post");
    }

    [Fact]
    public void Platform_ReturnsConfiguredValue()
    {
        var fetcher = new ManualFetcher(Platform.X);
        fetcher.Platform.Should().Be(Platform.X);
    }
}
