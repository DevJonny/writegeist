using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Core.Services;

public class StyleAnalyser
{
    private readonly IPostRepository _postRepository;
    private readonly IStyleProfileRepository _styleProfileRepository;
    private readonly ILlmProvider _llmProvider;

    public StyleAnalyser(
        IPostRepository postRepository,
        IStyleProfileRepository styleProfileRepository,
        ILlmProvider llmProvider)
    {
        _postRepository = postRepository;
        _styleProfileRepository = styleProfileRepository;
        _llmProvider = llmProvider;
    }

    public async Task<StyleProfile> AnalyseAsync(int personId)
    {
        var posts = await _postRepository.GetByPersonIdAsync(personId);

        if (posts.Count == 0)
            throw new InvalidOperationException(
                "No posts found for this person. Ingest some posts before analysing.");

        var prompt = BuildStyleAnalysisPrompt(posts);
        var profileJson = await _llmProvider.AnalyseStyleAsync(prompt);

        var profile = new StyleProfile
        {
            PersonId = personId,
            ProfileJson = profileJson,
            Provider = _llmProvider.ProviderName,
            Model = _llmProvider.ModelName,
            CreatedAt = DateTime.UtcNow
        };

        return await _styleProfileRepository.SaveAsync(profile);
    }

    internal static string BuildStyleAnalysisPrompt(IReadOnlyList<RawPost> posts)
    {
        var postsBlock = string.Join("\n\n---\n\n", posts.Select(p => p.Content));

        return """
            You are a writing style analyst. Analyse the following social media posts by the same author and produce a detailed, structured style profile in JSON format.

            ## Posts to analyse:

            """ + postsBlock + """


            ## Output format (JSON):

            {
              "vocabulary": {
                "complexity_level": "simple | moderate | advanced",
                "jargon_domains": ["tech", "marketing", ...],
                "favourite_words": ["word1", "word2", ...],
                "words_to_avoid": ["word1", ...],
                "filler_phrases": ["to be honest", "at the end of the day", ...]
              },
              "sentence_structure": {
                "average_length": "short | medium | long",
                "variety": "low | moderate | high",
                "fragment_usage": true/false,
                "question_usage": "none | rare | frequent",
                "exclamation_usage": "none | rare | frequent"
              },
              "paragraph_structure": {
                "average_length_sentences": 1-5,
                "uses_single_sentence_paragraphs": true/false,
                "uses_line_breaks_for_emphasis": true/false
              },
              "tone": {
                "formality": "casual | conversational | professional | formal",
                "warmth": "cold | neutral | warm | enthusiastic",
                "humour": "none | dry | playful | frequent",
                "confidence": "hedging | balanced | assertive | provocative"
              },
              "rhetorical_patterns": {
                "typical_opening": "description of how they start posts",
                "typical_closing": "description of how they end posts",
                "storytelling_tendency": "none | sometimes | often",
                "uses_lists": true/false,
                "uses_rhetorical_questions": true/false,
                "call_to_action_style": "none | soft | direct"
              },
              "formatting": {
                "emoji_usage": "none | rare | moderate | heavy",
                "emoji_types": ["🚀", "💡", ...],
                "hashtag_style": "none | minimal | moderate | heavy",
                "capitalisation_quirks": "none | ALL CAPS for emphasis | Title Case headers",
                "punctuation_quirks": "description of any unusual punctuation habits"
              },
              "content_patterns": {
                "typical_topics": ["topic1", "topic2"],
                "perspective": "first_person | third_person | mixed",
                "self_reference_style": "I | we | the team | name",
                "audience_address": "none | you | we | community"
              },
              "overall_voice_summary": "A 2-3 sentence summary of this person's writing voice that captures their essence."
            }

            Respond with only the JSON object, no other text.
            """;
    }
}
