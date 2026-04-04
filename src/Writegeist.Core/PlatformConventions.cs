using Writegeist.Core.Models;

namespace Writegeist.Core;

public static class PlatformConventions
{
    public static PlatformRules GetRules(Platform platform) => platform switch
    {
        Platform.LinkedIn => new PlatformRules(
            Name: "LinkedIn",
            MaxCharacters: 3000,
            RecommendedMaxLength: 1500,
            SupportsHashtags: true,
            RecommendedHashtagCount: 4,
            HashtagPlacement: "end",
            SupportsEmoji: true,
            EmojiGuidance: "Use sparingly for professional tone",
            ToneGuidance: "Professional, insightful, conversational",
            FormattingNotes: "Line breaks for readability; short paragraphs; hook in first line"),

        Platform.X => new PlatformRules(
            Name: "X",
            MaxCharacters: 280,
            RecommendedMaxLength: 280,
            SupportsHashtags: true,
            RecommendedHashtagCount: 2,
            HashtagPlacement: "inline",
            SupportsEmoji: true,
            EmojiGuidance: "Moderate use to add personality",
            ToneGuidance: "Concise, punchy, opinionated",
            FormattingNotes: "Single tweet; no threading assumed"),

        Platform.Instagram => new PlatformRules(
            Name: "Instagram",
            MaxCharacters: 2200,
            RecommendedMaxLength: 750,
            SupportsHashtags: true,
            RecommendedHashtagCount: 30,
            HashtagPlacement: "end in separate block",
            SupportsEmoji: true,
            EmojiGuidance: "Use freely to match visual culture",
            ToneGuidance: "Casual, authentic, visually descriptive",
            FormattingNotes: "Caption format; line breaks between sections; hashtag block at end"),

        Platform.Facebook => new PlatformRules(
            Name: "Facebook",
            MaxCharacters: 63206,
            RecommendedMaxLength: 600,
            SupportsHashtags: true,
            RecommendedHashtagCount: 2,
            HashtagPlacement: "end",
            SupportsEmoji: true,
            EmojiGuidance: "Moderate use for engagement",
            ToneGuidance: "Conversational, personal, community-oriented",
            FormattingNotes: "Short paragraphs; question at end to drive engagement"),

        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform")
    };
}
