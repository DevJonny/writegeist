namespace Writegeist.Core.Models;

public record PlatformRules(
    string Name,
    int? MaxCharacters,
    int RecommendedMaxLength,
    bool SupportsHashtags,
    int RecommendedHashtagCount,
    string HashtagPlacement,
    bool SupportsEmoji,
    string EmojiGuidance,
    string ToneGuidance,
    string FormattingNotes);
