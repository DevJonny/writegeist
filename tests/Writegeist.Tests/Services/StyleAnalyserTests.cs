using FluentAssertions;
using NSubstitute;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;
using Writegeist.Core.Services;

namespace Writegeist.Tests.Services;

public class StyleAnalyserTests
{
    private readonly IPostRepository _postRepository = Substitute.For<IPostRepository>();
    private readonly IStyleProfileRepository _styleProfileRepository = Substitute.For<IStyleProfileRepository>();
    private readonly ILlmProvider _llmProvider = Substitute.For<ILlmProvider>();
    private readonly StyleAnalyser _sut;

    public StyleAnalyserTests()
    {
        _sut = new StyleAnalyser(_postRepository, _styleProfileRepository, _llmProvider);
    }

    [Fact]
    public async Task AnalyseAsync_WithPosts_SendsPromptToLlmAndStoresProfile()
    {
        // Arrange
        var personId = 1;
        var posts = new List<RawPost>
        {
            new() { Id = 1, PersonId = personId, Content = "First post content", Platform = Platform.LinkedIn },
            new() { Id = 2, PersonId = personId, Content = "Second post content", Platform = Platform.LinkedIn }
        };

        _postRepository.GetByPersonIdAsync(personId).Returns(posts);
        _llmProvider.AnalyseStyleAsync(Arg.Any<string>()).Returns("{\"overall_voice_summary\": \"test\"}");
        _llmProvider.ProviderName.Returns("anthropic");
        _llmProvider.ModelName.Returns("claude-sonnet-4-20250514");

        _styleProfileRepository.SaveAsync(Arg.Any<StyleProfile>())
            .Returns(callInfo =>
            {
                var profile = callInfo.Arg<StyleProfile>();
                profile.Id = 42;
                return profile;
            });

        // Act
        var result = await _sut.AnalyseAsync(personId);

        // Assert
        result.Id.Should().Be(42);
        result.PersonId.Should().Be(personId);
        result.ProfileJson.Should().Be("{\"overall_voice_summary\": \"test\"}");
        result.Provider.Should().Be("anthropic");
        result.Model.Should().Be("claude-sonnet-4-20250514");

        await _llmProvider.Received(1).AnalyseStyleAsync(Arg.Is<string>(p =>
            p.Contains("First post content") && p.Contains("Second post content")));
        await _styleProfileRepository.Received(1).SaveAsync(Arg.Any<StyleProfile>());
    }

    [Fact]
    public async Task AnalyseAsync_WithNoPosts_ThrowsInvalidOperationException()
    {
        // Arrange
        _postRepository.GetByPersonIdAsync(1).Returns(new List<RawPost>());

        // Act
        var act = () => _sut.AnalyseAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No posts found*");
    }

    [Fact]
    public void BuildStyleAnalysisPrompt_IncludesAllPostContent()
    {
        // Arrange
        var posts = new List<RawPost>
        {
            new() { Content = "Post one" },
            new() { Content = "Post two" },
            new() { Content = "Post three" }
        };

        // Act
        var prompt = StyleAnalyser.BuildStyleAnalysisPrompt(posts);

        // Assert
        prompt.Should().Contain("Post one");
        prompt.Should().Contain("Post two");
        prompt.Should().Contain("Post three");
        prompt.Should().Contain("writing style analyst");
        prompt.Should().Contain("overall_voice_summary");
    }

    [Fact]
    public void BuildStyleAnalysisPrompt_SeparatesPostsWithDividers()
    {
        // Arrange
        var posts = new List<RawPost>
        {
            new() { Content = "Alpha" },
            new() { Content = "Beta" }
        };

        // Act
        var prompt = StyleAnalyser.BuildStyleAnalysisPrompt(posts);

        // Assert
        prompt.Should().Contain("Alpha\n\n---\n\nBeta");
    }
}
