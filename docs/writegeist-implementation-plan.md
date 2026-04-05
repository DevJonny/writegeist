# Writegeist — Implementation Plan

> A .NET CLI tool that clones a person's writing style from public social media posts and generates platform-specific content on demand.

---

## 1. Overview

Writegeist ingests public social media posts (LinkedIn, X/Twitter, Instagram, Facebook), analyses the author's writing style into a reusable profile, and generates new posts in that style for any supported platform. It supports both Anthropic (Claude) and OpenAI APIs as the LLM backend.

### Core Workflow

1. **Ingest** — Fetch public posts from a social profile URL/handle, or manually paste/import from a file.
2. **Analyse** — Send all ingested posts for a person to the LLM to extract a structured style profile. Store the profile in SQLite.
3. **Generate** — Given bullet points or a topic + a target platform, produce a single draft post using the style profile and platform-specific conventions.
4. **Refine** — Iterate on the last generated draft with natural language feedback, preserving style consistency.

---

## 2. Tech Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 10 (latest preview or stable, whichever is current) |
| Project type | Console app using `DevJonny.InteractiveCli` (interactive menu-driven CLI) |
| CLI framework | `DevJonny.InteractiveCli` — menu/action based interactive CLI built on Spectre.Console + CommandLineParser |
| Database | SQLite via `Microsoft.Data.Sqlite` + Dapper or raw ADO.NET |
| HTTP | `HttpClient` via `IHttpClientFactory` |
| HTML parsing (scraping) | `AngleSharp` |
| JSON serialisation | `System.Text.Json` (STJ) |
| LLM: Anthropic | `Anthropic` NuGet package (official C# SDK) — verify this exists; if not, use raw HTTP to `https://api.anthropic.com/v1/messages` |
| LLM: OpenAI | `OpenAI` NuGet package (official) or `Azure.AI.OpenAI` |
| DI | `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Hosting` (Generic Host) — provided by InteractiveCli |
| Configuration | `Microsoft.Extensions.Configuration` (appsettings.json + env vars + user-secrets) |
| Logging | `Serilog` (provided by InteractiveCli) |

---

## 3. Solution Structure

```
Writegeist/
├── Writegeist.sln
├── src/
│   ├── Writegeist.Cli/                      # Console app entry point
│   │   ├── Program.cs                       # Host builder + InteractiveCli bootstrapping
│   │   ├── Menus/
│   │   │   ├── MainMenu.cs                  # Top-level menu (isTopLevel: true)
│   │   │   ├── IngestMenu.cs                # Sub-menu for ingestion options
│   │   │   └── ProfileMenu.cs               # Sub-menu for profile management
│   │   ├── Actions/
│   │   │   ├── IngestFromFileAction.cs       # SingleActionAsync — ingest from file
│   │   │   ├── IngestInteractiveAction.cs    # RepeatableActionAsync — paste posts interactively
│   │   │   ├── IngestFromUrlAction.cs        # SingleActionAsync — ingest from URL/handle
│   │   │   ├── AnalyseAction.cs             # SingleActionAsync — analyse style
│   │   │   ├── GenerateAction.cs            # SingleActionAsync — generate a post
│   │   │   ├── RefineAction.cs              # RepeatableActionAsync — refine loop
│   │   │   ├── ShowProfileAction.cs         # SingleActionAsync — display a profile
│   │   │   └── ListProfilesAction.cs        # SingleActionAsync — list all profiles
│   │   └── appsettings.json
│   │
│   ├── Writegeist.Core/                     # Domain logic, interfaces, models
│   │   ├── Models/
│   │   │   ├── Person.cs
│   │   │   ├── RawPost.cs
│   │   │   ├── StyleProfile.cs
│   │   │   ├── GeneratedDraft.cs
│   │   │   └── Platform.cs                  # enum: LinkedIn, X, Instagram, Facebook
│   │   ├── Interfaces/
│   │   │   ├── IContentFetcher.cs           # Per-platform content ingestion
│   │   │   ├── ILlmProvider.cs              # Analyse / Generate / Refine
│   │   │   ├── IStyleProfileRepository.cs
│   │   │   ├── IPostRepository.cs
│   │   │   └── IPersonRepository.cs
│   │   └── Services/
│   │       ├── StyleAnalyser.cs             # Orchestrates style profile creation
│   │       ├── PostGenerator.cs             # Orchestrates post generation
│   │       └── PlatformConventions.cs       # Platform-specific rules
│   │
│   ├── Writegeist.Infrastructure/           # Implementations
│   │   ├── Fetchers/
│   │   │   ├── LinkedInFetcher.cs
│   │   │   ├── XTwitterFetcher.cs
│   │   │   ├── InstagramFetcher.cs
│   │   │   ├── FacebookFetcher.cs
│   │   │   └── ManualFetcher.cs             # File/interactive paste
│   │   ├── LlmProviders/
│   │   │   ├── AnthropicProvider.cs
│   │   │   └── OpenAiProvider.cs
│   │   └── Persistence/
│   │       ├── SqliteDatabase.cs            # Schema init + migrations
│   │       ├── SqlitePersonRepository.cs
│   │       ├── SqlitePostRepository.cs
│   │       └── SqliteStyleProfileRepository.cs
│   │
│   └── Writegeist.Tests/                    # xUnit test project
│       ├── Services/
│       │   ├── StyleAnalyserTests.cs
│       │   ├── PostGeneratorTests.cs
│       │   └── PlatformConventionsTests.cs
│       ├── Fetchers/
│       │   └── ManualFetcherTests.cs
│       └── Persistence/
│           └── SqliteRepositoryTests.cs
```

---

## 4. Interactive CLI Design

Writegeist uses `DevJonny.InteractiveCli` (NuGet: `DevJonny.InteractiveCli`) which provides a menu-driven interactive experience built on Spectre.Console. Instead of traditional CLI commands with flags, each workflow step is an **action** presented via interactive menus. User input (person name, platform, topic, etc.) is collected via Spectre.Console prompts within each action.

### 4.1 Menu Structure

```
Main Menu
├── Ingest Posts                    → IngestMenu
│   ├── From File                  → IngestFromFileAction
│   ├── From URL / Handle          → IngestFromUrlAction
│   ├── Interactive Paste          → IngestInteractiveAction
│   └── Back
├── Analyse Style                  → AnalyseAction
├── Generate Post                  → GenerateAction
├── Refine Last Draft              → RefineAction
├── Profiles                       → ProfileMenu
│   ├── Show Profile               → ShowProfileAction
│   ├── List All Profiles          → ListProfilesAction
│   └── Back
└── Quit
```

### 4.2 Program.cs (Entry Point)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using InteractiveCLI;
using Serilog;
using Writegeist.Cli.Menus;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateBootstrapLogger();

var host =
    Host.CreateDefaultBuilder()
        .AddInteractiveCli(configuration, services =>
        {
            // Register Writegeist-specific services (repositories, LLM providers, fetchers, etc.)
        })
        .Build();

host.UseInteractiveCli(_ => new MainMenu(), args);
```

### 4.3 Actions Detail

Each action extends `SingleActionAsync` or `RepeatableActionAsync` from InteractiveCLI and collects input via Spectre.Console prompts.

#### IngestFromFileAction (SingleActionAsync)

Prompts for:
- Person name (text prompt)
- Platform (selection prompt: LinkedIn, X, Instagram, Facebook)
- File path (text prompt)

Behaviour:
- Creates the person in SQLite if they don't exist.
- Reads the file, splits on `---` separators.
- Stores each post as a `RawPost` with SHA-256 content hash for dedup.
- Prints summary: "Ingested 14 new posts for John Smith from LinkedIn (3 duplicates skipped)."

#### IngestInteractiveAction (RepeatableActionAsync)

Prompts for:
- Person name (text prompt, on first iteration)
- Platform (selection prompt, on first iteration)
- Post content (multi-line text prompt)

Returns `false` to keep looping, `true` when user confirms they're done. Stores each post as it's entered.

#### IngestFromUrlAction (SingleActionAsync)

Prompts for:
- Person name (text prompt)
- Platform (selection prompt)
- URL or handle (text prompt)

Dispatches to the appropriate `IContentFetcher`. If the platform doesn't support automated fetching, displays a warning and suggests using File or Interactive instead.

#### AnalyseAction (SingleActionAsync)

Prompts for:
- Person name (selection prompt from existing persons)
- LLM provider (selection prompt: Anthropic, OpenAI — defaults from config)

Behaviour:
- Loads all `RawPost` records for the person.
- Sends them to the LLM with the style analysis prompt (see §7.1).
- Stores the resulting `StyleProfile` JSON in SQLite, versioned with a timestamp.
- Prints the profile summary to the console.

#### GenerateAction (SingleActionAsync)

Prompts for:
- Person name (selection prompt from persons with a style profile)
- Target platform (selection prompt)
- Topic/key points (multi-line text prompt, or file path option)
- LLM provider (selection prompt, optional override)

Behaviour:
- Loads the latest `StyleProfile` for the person.
- Loads platform conventions for the target platform (see §6).
- Sends the generation prompt (see §7.2) to the LLM.
- Stores the result as a `GeneratedDraft` in SQLite.
- Prints the generated post to stdout.

#### RefineAction (RepeatableActionAsync)

Prompts for:
- Feedback (text prompt)

Behaviour:
- Loads the most recent `GeneratedDraft` (and its associated style profile + platform).
- Sends the refinement prompt (see §7.3) with the previous draft + feedback.
- Stores the new version as a `GeneratedDraft` linked to the previous one.
- Prints the refined post.
- Asks "Refine again?" — returns `true` to stop, `false` to continue looping.

#### ShowProfileAction (SingleActionAsync)

Prompts for:
- Person name (selection prompt)

Displays the full style profile in a formatted table.

#### ListProfilesAction (SingleActionAsync)

Lists all persons with style profiles in a table.

---

## 5. Data Model (SQLite)

### Schema

```sql
CREATE TABLE IF NOT EXISTS persons (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS raw_posts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    person_id INTEGER NOT NULL REFERENCES persons(id),
    platform TEXT NOT NULL,           -- 'linkedin', 'x', 'instagram', 'facebook'
    content TEXT NOT NULL,
    content_hash TEXT NOT NULL,        -- SHA-256 for dedup
    source_url TEXT,
    fetched_at TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(person_id, content_hash)
);

CREATE TABLE IF NOT EXISTS style_profiles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    person_id INTEGER NOT NULL REFERENCES persons(id),
    profile_json TEXT NOT NULL,        -- Structured style profile as JSON
    provider TEXT NOT NULL,            -- 'anthropic' or 'openai'
    model TEXT NOT NULL,               -- e.g. 'claude-sonnet-4-20250514'
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS generated_drafts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    person_id INTEGER NOT NULL REFERENCES persons(id),
    style_profile_id INTEGER NOT NULL REFERENCES style_profiles(id),
    platform TEXT NOT NULL,
    topic TEXT NOT NULL,
    content TEXT NOT NULL,
    parent_draft_id INTEGER REFERENCES generated_drafts(id),  -- NULL for first draft, set for refinements
    feedback TEXT,                     -- refinement feedback (NULL for first draft)
    provider TEXT NOT NULL,
    model TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

Run schema creation on first use via `SqliteDatabase.EnsureCreated()`.

---

## 6. Platform Conventions

Encapsulate in `PlatformConventions.cs` as a static lookup. Each platform defines:

```csharp
public record PlatformRules(
    string Name,
    int? MaxCharacters,          // null = no hard limit
    int RecommendedMaxLength,    // soft target in characters
    bool SupportsHashtags,
    int RecommendedHashtagCount, // 0 if not typical
    string HashtagPlacement,     // "inline", "end", "none"
    bool SupportsEmoji,
    string EmojiGuidance,        // "sparingly", "freely", "none"
    string ToneGuidance,         // platform-typical tone advice
    string FormattingNotes        // line breaks, paragraphs, etc.
);
```

### Platform defaults:

**LinkedIn:**
- Max: 3,000 characters
- Recommended: 1,200–1,800 chars
- Hashtags: 3–5 at the end
- Emoji: sparingly (bullet replacements, single opener)
- Tone: professional but personal, thought-leadership
- Formatting: short paragraphs (1–2 sentences), liberal line breaks, hook in first line

**X/Twitter:**
- Max: 280 characters (single tweet) or 25,000 (long-form post)
- Default to 280 unless topic clearly needs a thread
- Hashtags: 1–2 inline
- Emoji: moderate
- Tone: conversational, concise, punchy
- Formatting: no paragraph breaks in single tweets

**Instagram:**
- Max: 2,200 characters
- Recommended: 500–1,000 chars
- Hashtags: up to 30, in a separate block at the end (after line breaks)
- Emoji: freely
- Tone: casual, visual-language, storytelling
- Formatting: line breaks for readability, can use dot separators for spacing

**Facebook:**
- Max: 63,206 characters
- Recommended: 400–800 chars
- Hashtags: 1–3 or none
- Emoji: moderate
- Tone: conversational, community-oriented
- Formatting: short paragraphs, conversational

---

## 7. LLM Prompts

### 7.1 Style Analysis Prompt

```
You are a writing style analyst. Analyse the following social media posts by the same author and produce a detailed, structured style profile in JSON format.

## Posts to analyse:

{posts_block}

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
```

### 7.2 Generation Prompt

```
You are a ghostwriter. Write a {platform} post in the exact writing style described by the style profile below. The post should be about the topic/points provided.

## Style Profile:
{style_profile_json}

## Platform Conventions:
- Platform: {platform_name}
- Character limit: {max_chars}
- Recommended length: {recommended_length}
- Hashtags: {hashtag_guidance}
- Emoji: {emoji_guidance}
- Formatting: {formatting_notes}

## Topic / Key Points:
{topic}

## Instructions:
- Match the voice, tone, vocabulary, and structural patterns from the style profile exactly
- Follow the platform conventions for formatting, length, and hashtag/emoji usage
- Do NOT add disclaimers, meta-commentary, or explain what you're doing
- Output ONLY the post text, ready to copy and paste
```

### 7.3 Refinement Prompt

```
You are a ghostwriter refining a draft. Adjust the post below according to the feedback while maintaining the original writing style.

## Style Profile:
{style_profile_json}

## Platform: {platform_name}
## Platform Conventions:
{platform_conventions}

## Current Draft:
{current_draft}

## Feedback:
{feedback}

## Instructions:
- Apply the feedback while staying true to the style profile
- Maintain platform conventions
- Output ONLY the revised post text
```

---

## 8. Key Interfaces

### IContentFetcher

```csharp
public interface IContentFetcher
{
    Platform Platform { get; }
    Task<IReadOnlyList<FetchedPost>> FetchPostsAsync(
        FetchRequest request,
        CancellationToken cancellationToken = default);
}

public record FetchRequest(
    string? Url = null,
    string? Handle = null,
    string? FilePath = null);

public record FetchedPost(
    string Content,
    string? SourceUrl = null,
    DateTimeOffset? PublishedAt = null);
```

### ILlmProvider

```csharp
public interface ILlmProvider
{
    string ProviderName { get; }  // "anthropic" or "openai"
    string ModelName { get; }

    Task<string> AnalyseStyleAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePostAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    Task<string> RefinePostAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
```

---

## 9. Configuration

### appsettings.json

```json
{
  "Writegeist": {
    "DefaultProvider": "anthropic",
    "DatabasePath": "writegeist.db",
    "Anthropic": {
      "Model": "claude-sonnet-4-20250514"
    },
    "OpenAi": {
      "Model": "gpt-4o"
    }
  }
}
```

### Environment Variables

```
ANTHROPIC_API_KEY=sk-ant-...
OPENAI_API_KEY=sk-...
```

Use `IConfiguration` with the standard provider chain: appsettings.json → environment variables → user secrets (dev).

---

## 10. Content Fetcher Implementation Notes

### General Architecture

Each fetcher implements `IContentFetcher`. Register all of them in DI alongside the InteractiveCli action auto-registration. Resolve by `Platform` enum using a factory or keyed services (.NET 8+).

### LinkedIn

- **API**: LinkedIn's API requires OAuth2 and an approved app; post content is generally NOT available via the standard API. Not viable for v1.
- **Scraping**: LinkedIn aggressively blocks unauthenticated scraping (login walls, bot detection).
- **v1 approach**: `LinkedInFetcher` should log a warning that automated fetching is not available and suggest using File or Interactive paste instead. Implement as a stub that throws a user-friendly exception.
- **Future**: Could integrate a headless browser (Playwright) with cookie-based auth, but this is fragile and potentially ToS-violating.

### X/Twitter

- **API**: X API v2 (free tier) allows fetching recent tweets by user ID. Requires a bearer token.
- **Endpoint**: `GET /2/users/{id}/tweets` — returns up to 3,200 most recent tweets.
- **Auth**: Bearer token via `X_BEARER_TOKEN` env var.
- **v1 approach**: Implement API-based fetching. Fall back to manual if no token configured.
- **Rate limits**: 1 request per 15 minutes on free tier (returns up to 100 tweets per request). Handle 429 gracefully.

### Instagram

- **API**: Meta Graph API requires a Facebook app + Instagram Business/Creator account connection. Not viable for fetching arbitrary public profiles.
- **Scraping**: Instagram blocks most scraping. Public profile JSON endpoints have been locked down.
- **v1 approach**: Stub with manual fallback, similar to LinkedIn.
- **Future**: Playwright-based approach or third-party services.

### Facebook

- **API**: Graph API can access public Page posts (not personal profiles) with a Page Access Token.
- **Scraping**: Heavily blocked.
- **v1 approach**: Stub with manual fallback for personal profiles. Could implement Page post fetching if a Page Access Token is configured.

### ManualFetcher

- Reads from a file where posts are separated by `---` on its own line.
- Or runs an interactive loop: user pastes a post, presses Enter twice (blank line) to submit, types `done` to finish.
- Always available as fallback regardless of platform.

---

## 11. Build & Run

```bash
dotnet new sln -n Writegeist
dotnet new console -n Writegeist.Cli -o src/Writegeist.Cli
dotnet new classlib -n Writegeist.Core -o src/Writegeist.Core
dotnet new classlib -n Writegeist.Infrastructure -o src/Writegeist.Infrastructure
dotnet new xunit -n Writegeist.Tests -o src/Writegeist.Tests

dotnet sln add src/Writegeist.Cli
dotnet sln add src/Writegeist.Core
dotnet sln add src/Writegeist.Infrastructure
dotnet sln add src/Writegeist.Tests

# Project references
cd src/Writegeist.Cli && dotnet add reference ../Writegeist.Core ../Writegeist.Infrastructure
cd ../Writegeist.Infrastructure && dotnet add reference ../Writegeist.Core
cd ../Writegeist.Tests && dotnet add reference ../Writegeist.Core ../Writegeist.Infrastructure
```

### NuGet Packages

**Writegeist.Cli:**
- `DevJonny.InteractiveCli` (brings in Spectre.Console, CommandLineParser, Serilog, Microsoft.Extensions.Hosting)

**Writegeist.Infrastructure:**
- `Microsoft.Data.Sqlite`
- `Dapper` (optional, or use raw ADO.NET)
- `AngleSharp` (HTML parsing for scraping)
- `Anthropic` (if official SDK exists; otherwise raw HttpClient)
- `OpenAI`

**Writegeist.Tests:**
- `xunit`
- `NSubstitute` or `Moq`
- `FluentAssertions`

---

## 12. Implementation Order

Build in this sequence — each step is independently testable:

1. **Solution scaffolding** — Create projects, references, NuGet packages. Wire up InteractiveCli bootstrapper with a MainMenu that has placeholder actions.
2. **Data model + SQLite persistence** — `Models/`, `SqliteDatabase.cs`, repositories. Write tests.
3. **ManualFetcher** — File and interactive ingestion. Write tests.
4. **Ingest actions** — Wire up IngestMenu → IngestFromFileAction, IngestInteractiveAction → ManualFetcher → SQLite.
5. **ILlmProvider + AnthropicProvider** — Implement API calls with the style analysis prompt.
6. **StyleAnalyser service + AnalyseAction** — Orchestrate analysis, store profile.
7. **PlatformConventions** — Static platform rules.
8. **PostGenerator service + GenerateAction** — Generation with style profile + platform rules.
9. **RefineAction** — Refinement loop using RepeatableActionAsync.
10. **ProfileMenu + actions** — ShowProfileAction, ListProfilesAction.
11. **OpenAiProvider** — Second LLM backend.
12. **XTwitterFetcher + IngestFromUrlAction** — API-based tweet fetching (if X API access is available).
13. **Stub fetchers** — LinkedIn, Instagram, Facebook stubs with helpful error messages.
14. **Polish** — Error handling, logging, help text, README.

---

## 13. Testing Strategy

- **Unit tests**: Style analyser, post generator, platform conventions, repositories (in-memory SQLite).
- **Integration tests**: Full action execution with a mock LLM provider (return canned responses).
- **Manual smoke tests**: End-to-end with real API keys against both providers.
- Mock `ILlmProvider` and `IContentFetcher` in tests — never call real APIs in automated tests.

---

## 14. Future Enhancements (Out of Scope for v1)

- Headless browser fetching (Playwright) for LinkedIn/Instagram
- Thread generation for X (multi-tweet)
- Image caption generation for Instagram
- A/B style comparison (generate from two different people's styles)
- Export to clipboard / direct posting via APIs
- Web UI wrapper
- Style drift detection (re-analyse and compare profiles over time)
