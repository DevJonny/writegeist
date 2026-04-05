using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Core.Services;

public class PostGenerator
{
    private readonly IStyleProfileRepository _styleProfileRepository;
    private readonly IDraftRepository _draftRepository;
    private readonly ILlmProvider _llmProvider;

    public PostGenerator(
        IStyleProfileRepository styleProfileRepository,
        IDraftRepository draftRepository,
        ILlmProvider llmProvider)
    {
        _styleProfileRepository = styleProfileRepository;
        _draftRepository = draftRepository;
        _llmProvider = llmProvider;
    }

    public async Task<GeneratedDraft> GenerateAsync(int personId, Platform platform, string topic)
    {
        var profile = await _styleProfileRepository.GetLatestByPersonIdAsync(personId)
                      ?? throw new InvalidOperationException(
                          "No style profile found for this person. Run 'Analyse Style' first.");

        var rules = PlatformConventions.GetRules(platform);
        var prompt = BuildGenerationPrompt(profile.ProfileJson, rules, topic);
        var content = await _llmProvider.GeneratePostAsync(prompt);

        var draft = new GeneratedDraft
        {
            PersonId = personId,
            StyleProfileId = profile.Id,
            Platform = platform,
            Topic = topic,
            Content = content,
            Provider = _llmProvider.ProviderName,
            Model = _llmProvider.ModelName,
            CreatedAt = DateTime.UtcNow
        };

        return await _draftRepository.SaveAsync(draft);
    }

    public async Task<GeneratedDraft> RefineAsync(int draftId, string feedback)
    {
        var previousDraft = await _draftRepository.GetByIdAsync(draftId)
                            ?? throw new InvalidOperationException("Draft not found.");

        var profile = await _styleProfileRepository.GetLatestByPersonIdAsync(previousDraft.PersonId)
                      ?? throw new InvalidOperationException("Style profile not found.");

        var rules = PlatformConventions.GetRules(previousDraft.Platform);
        var prompt = BuildRefinementPrompt(profile.ProfileJson, rules, previousDraft.Content, feedback);
        var content = await _llmProvider.RefinePostAsync(prompt);

        var refined = new GeneratedDraft
        {
            PersonId = previousDraft.PersonId,
            StyleProfileId = profile.Id,
            Platform = previousDraft.Platform,
            Topic = previousDraft.Topic,
            Content = content,
            ParentDraftId = draftId,
            Feedback = feedback,
            Provider = _llmProvider.ProviderName,
            Model = _llmProvider.ModelName,
            CreatedAt = DateTime.UtcNow
        };

        return await _draftRepository.SaveAsync(refined);
    }

    internal static string BuildGenerationPrompt(string styleProfileJson, PlatformRules rules, string topic)
    {
        return $"""
            You are a ghostwriter. Write a {rules.Name} post in the exact writing style described by the style profile below. The post should be about the topic/points provided.

            ## Style Profile:
            {styleProfileJson}

            ## Platform Conventions:
            - Platform: {rules.Name}
            - Character limit: {rules.MaxCharacters?.ToString() ?? "none"}
            - Recommended length: {rules.RecommendedMaxLength} characters
            - Hashtags: {rules.RecommendedHashtagCount} hashtags, placement: {rules.HashtagPlacement}
            - Emoji: {rules.EmojiGuidance}
            - Formatting: {rules.FormattingNotes}

            ## Topic / Key Points:
            {topic}

            ## Instructions:
            - Match the voice, tone, vocabulary, and structural patterns from the style profile exactly
            - Follow the platform conventions for formatting, length, and hashtag/emoji usage
            - Do NOT add disclaimers, meta-commentary, or explain what you're doing
            - Output ONLY the post text, ready to copy and paste
            """;
    }

    internal static string BuildRefinementPrompt(
        string styleProfileJson, PlatformRules rules, string currentDraft, string feedback)
    {
        return $"""
            You are a ghostwriter refining a draft. Adjust the post below according to the feedback while maintaining the original writing style.

            ## Style Profile:
            {styleProfileJson}

            ## Platform: {rules.Name}
            ## Platform Conventions:
            - Character limit: {rules.MaxCharacters?.ToString() ?? "none"}
            - Recommended length: {rules.RecommendedMaxLength} characters
            - Hashtags: {rules.RecommendedHashtagCount} hashtags, placement: {rules.HashtagPlacement}
            - Emoji: {rules.EmojiGuidance}
            - Formatting: {rules.FormattingNotes}

            ## Current Draft:
            {currentDraft}

            ## Feedback:
            {feedback}

            ## Instructions:
            - Apply the feedback while staying true to the style profile
            - Maintain platform conventions
            - Output ONLY the revised post text
            """;
    }
}
