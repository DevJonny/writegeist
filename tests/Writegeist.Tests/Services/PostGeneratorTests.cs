using FluentAssertions;
using NSubstitute;
using Writegeist.Core;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;
using Writegeist.Core.Services;

namespace Writegeist.Tests.Services;

public class PostGeneratorTests
{
    private readonly IStyleProfileRepository _styleProfileRepository = Substitute.For<IStyleProfileRepository>();
    private readonly IDraftRepository _draftRepository = Substitute.For<IDraftRepository>();
    private readonly ILlmProvider _llmProvider = Substitute.For<ILlmProvider>();
    private readonly PostGenerator _sut;

    public PostGeneratorTests()
    {
        _sut = new PostGenerator(_styleProfileRepository, _draftRepository, _llmProvider);
    }

    [Fact]
    public async Task GenerateAsync_WithProfile_BuildsPromptAndStoresDraft()
    {
        // Arrange
        var personId = 1;
        var profile = new StyleProfile
        {
            Id = 10,
            PersonId = personId,
            ProfileJson = "{\"tone\": \"professional\"}"
        };

        _styleProfileRepository.GetLatestByPersonIdAsync(personId).Returns(profile);
        _llmProvider.GeneratePostAsync(Arg.Any<string>()).Returns("Generated post content");
        _llmProvider.ProviderName.Returns("anthropic");
        _llmProvider.ModelName.Returns("claude-sonnet-4-20250514");

        _draftRepository.SaveAsync(Arg.Any<GeneratedDraft>())
            .Returns(callInfo =>
            {
                var draft = callInfo.Arg<GeneratedDraft>();
                draft.Id = 99;
                return draft;
            });

        // Act
        var result = await _sut.GenerateAsync(personId, Platform.LinkedIn, "My topic");

        // Assert
        result.Id.Should().Be(99);
        result.PersonId.Should().Be(personId);
        result.StyleProfileId.Should().Be(10);
        result.Platform.Should().Be(Platform.LinkedIn);
        result.Topic.Should().Be("My topic");
        result.Content.Should().Be("Generated post content");
        result.ParentDraftId.Should().BeNull();
        result.Provider.Should().Be("anthropic");

        await _llmProvider.Received(1).GeneratePostAsync(Arg.Is<string>(p =>
            p.Contains("My topic") && p.Contains("professional") && p.Contains("LinkedIn")));
    }

    [Fact]
    public async Task GenerateAsync_WithNoProfile_ThrowsInvalidOperationException()
    {
        _styleProfileRepository.GetLatestByPersonIdAsync(1).Returns((StyleProfile?)null);

        var act = () => _sut.GenerateAsync(1, Platform.LinkedIn, "topic");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No style profile found*");
    }

    [Fact]
    public async Task RefineAsync_WithExistingDraft_BuildsPromptAndStoresLinkedDraft()
    {
        // Arrange
        var previousDraft = new GeneratedDraft
        {
            Id = 5,
            PersonId = 1,
            StyleProfileId = 10,
            Platform = Platform.X,
            Topic = "Original topic",
            Content = "Original draft content"
        };

        var profile = new StyleProfile
        {
            Id = 10,
            PersonId = 1,
            ProfileJson = "{\"tone\": \"casual\"}"
        };

        _draftRepository.GetByIdAsync(5).Returns(previousDraft);
        _styleProfileRepository.GetLatestByPersonIdAsync(1).Returns(profile);
        _llmProvider.RefinePostAsync(Arg.Any<string>()).Returns("Refined post content");
        _llmProvider.ProviderName.Returns("openai");
        _llmProvider.ModelName.Returns("gpt-4o");

        _draftRepository.SaveAsync(Arg.Any<GeneratedDraft>())
            .Returns(callInfo =>
            {
                var draft = callInfo.Arg<GeneratedDraft>();
                draft.Id = 6;
                return draft;
            });

        // Act
        var result = await _sut.RefineAsync(5, "Make it shorter");

        // Assert
        result.Id.Should().Be(6);
        result.ParentDraftId.Should().Be(5);
        result.Feedback.Should().Be("Make it shorter");
        result.Content.Should().Be("Refined post content");
        result.Platform.Should().Be(Platform.X);
        result.Topic.Should().Be("Original topic");

        await _llmProvider.Received(1).RefinePostAsync(Arg.Is<string>(p =>
            p.Contains("Original draft content") && p.Contains("Make it shorter") && p.Contains("casual")));
    }

    [Fact]
    public async Task RefineAsync_WithNonExistentDraft_ThrowsInvalidOperationException()
    {
        _draftRepository.GetByIdAsync(999).Returns((GeneratedDraft?)null);

        var act = () => _sut.RefineAsync(999, "feedback");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft not found*");
    }

    [Fact]
    public void BuildGenerationPrompt_IncludesAllComponents()
    {
        var rules = PlatformConventions.GetRules(Platform.LinkedIn);
        var prompt = PostGenerator.BuildGenerationPrompt("{\"style\":true}", rules, "My topic here");

        prompt.Should().Contain("ghostwriter");
        prompt.Should().Contain("{\"style\":true}");
        prompt.Should().Contain("LinkedIn");
        prompt.Should().Contain("My topic here");
        prompt.Should().Contain("3000");
    }

    [Fact]
    public void BuildRefinementPrompt_IncludesAllComponents()
    {
        var rules = PlatformConventions.GetRules(Platform.X);
        var prompt = PostGenerator.BuildRefinementPrompt("{\"style\":true}", rules, "Draft text", "Make punchier");

        prompt.Should().Contain("refining a draft");
        prompt.Should().Contain("{\"style\":true}");
        prompt.Should().Contain("Draft text");
        prompt.Should().Contain("Make punchier");
        prompt.Should().Contain("280");
    }
}
