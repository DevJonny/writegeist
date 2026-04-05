# PRD: Writegeist

## Introduction

Writegeist is a .NET interactive CLI tool that learns a person's writing style from their social media posts and generates new platform-specific content in that style. It solves the problem of maintaining authentic, consistent voice across social platforms — you feed it your existing posts, it builds a reusable style profile, and then ghostwrites new posts that sound like you.

The tool uses an interactive menu-driven interface (via DevJonny.InteractiveCli) with polished Spectre.Console output, supports both Anthropic and OpenAI as LLM backends, and stores all data locally in SQLite.

## Goals

- Clone the user's personal writing style from existing social media posts into a reusable profile
- Generate platform-aware drafts (LinkedIn, X, Instagram, Facebook) that match the user's voice
- Support iterative refinement of generated drafts with natural language feedback
- Provide a polished interactive CLI experience with tables, panels, colours, and spinners
- Support both Anthropic (Claude) and OpenAI as LLM providers from day one
- Support all four platforms with automated fetching where possible and manual fallbacks where not

## User Stories

### US-001: Solution scaffolding
**Description:** As a developer, I need the .NET solution structure created with all projects, references, and NuGet packages so that I can start building features.

**Acceptance Criteria:**
- [ ] `Writegeist.sln` with four projects: `Writegeist.Cli`, `Writegeist.Core`, `Writegeist.Infrastructure`, `Writegeist.Tests`
- [ ] Project references wired: Cli → Core + Infrastructure, Infrastructure → Core, Tests → Core + Infrastructure
- [ ] `Writegeist.Cli` references `DevJonny.InteractiveCli` NuGet package
- [ ] `Writegeist.Infrastructure` references `Microsoft.Data.Sqlite`, `AngleSharp`, `Anthropic` (or raw HTTP fallback), `OpenAI`
- [ ] `Writegeist.Tests` references `xunit`, `FluentAssertions`, and a mocking library
- [ ] `Program.cs` bootstraps InteractiveCli with a MainMenu that displays and quits cleanly
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet run` launches the interactive menu

### US-002: SQLite database and schema
**Description:** As a developer, I need a SQLite database with the schema for persons, raw posts, style profiles, and generated drafts so that all data persists locally.

**Acceptance Criteria:**
- [ ] `SqliteDatabase.EnsureCreated()` creates the database file and all four tables (`persons`, `raw_posts`, `style_profiles`, `generated_drafts`)
- [ ] Schema matches the data model spec (auto-increment IDs, foreign keys, unique constraints, defaults)
- [ ] Database path is configurable via `appsettings.json` (`Writegeist:DatabasePath`)
- [ ] Tables are created idempotently (safe to call multiple times)
- [ ] Unit tests verify schema creation with in-memory SQLite

### US-003: Person repository
**Description:** As a developer, I need CRUD operations for persons so that the ingest and analyse workflows can store and retrieve people.

**Acceptance Criteria:**
- [ ] `IPersonRepository` interface in Core with methods: `CreateAsync`, `GetByNameAsync`, `GetAllAsync`, `GetOrCreateAsync`
- [ ] `SqlitePersonRepository` implementation in Infrastructure
- [ ] `GetOrCreateAsync` returns existing person if name matches (case-insensitive), creates if not
- [ ] Unit tests with in-memory SQLite cover create, get, get-all, and dedup scenarios

### US-004: Post repository
**Description:** As a developer, I need CRUD operations for raw posts so that ingested content can be stored and retrieved per person.

**Acceptance Criteria:**
- [ ] `IPostRepository` interface in Core with methods: `AddAsync`, `GetByPersonIdAsync`, `GetCountByPersonIdAsync`, `ExistsByHashAsync`
- [ ] `SqlitePostRepository` implementation in Infrastructure
- [ ] Posts are deduplicated by SHA-256 content hash per person (unique constraint on `person_id` + `content_hash`)
- [ ] `AddAsync` returns a result indicating whether the post was new or a duplicate
- [ ] Unit tests cover add, dedup, and retrieval

### US-005: Style profile repository
**Description:** As a developer, I need storage for style profiles so that analysed profiles persist and can be retrieved for generation.

**Acceptance Criteria:**
- [ ] `IStyleProfileRepository` interface in Core with methods: `SaveAsync`, `GetLatestByPersonIdAsync`, `GetAllByPersonIdAsync`
- [ ] `SqliteStyleProfileRepository` implementation in Infrastructure
- [ ] `GetLatestByPersonIdAsync` returns the most recent profile by `created_at`
- [ ] Profile JSON stored as text, provider and model recorded
- [ ] Unit tests cover save, retrieve latest, and retrieve all

### US-006: Draft repository
**Description:** As a developer, I need storage for generated drafts so that the refine workflow can retrieve and chain drafts.

**Acceptance Criteria:**
- [ ] Draft repository interface with methods: `SaveAsync`, `GetLatestAsync`, `GetByIdAsync`
- [ ] `GetLatestAsync` returns the most recently created draft (across all persons)
- [ ] `parent_draft_id` links refined drafts to their predecessors
- [ ] Unit tests cover save, retrieval, and parent-child linking

### US-007: Manual fetcher — file import
**Description:** As a user, I want to import my posts from a text file so that I can feed Writegeist content from any platform without needing API access.

**Acceptance Criteria:**
- [ ] `ManualFetcher` implements `IContentFetcher`
- [ ] Reads a text file and splits posts on `---` separator lines
- [ ] Trims whitespace from each post, skips empty entries
- [ ] Returns a list of `FetchedPost` records
- [ ] Unit tests cover normal file, empty file, file with no separators, and file with consecutive separators

### US-008: Manual fetcher — interactive paste
**Description:** As a user, I want to paste posts one at a time in an interactive session so that I can quickly add content without creating a file.

**Acceptance Criteria:**
- [ ] Interactive mode uses Spectre.Console `TextPrompt` for multi-line input
- [ ] User pastes a post, confirms, then is asked if they want to add another
- [ ] Session ends when user chooses to stop
- [ ] Each post is returned as a `FetchedPost`

### US-009: Ingest menu and actions
**Description:** As a user, I want a sub-menu for ingesting posts with options for file import, URL/handle fetch, and interactive paste so that I can choose my preferred input method.

**Acceptance Criteria:**
- [ ] `IngestMenu` appears as "Ingest Posts" in the MainMenu
- [ ] Sub-menu contains: "From File", "From URL / Handle", "Interactive Paste", "Back"
- [ ] `IngestFromFileAction` prompts for person name, platform (selection), and file path
- [ ] `IngestInteractiveAction` prompts for person name and platform, then loops for posts
- [ ] `IngestFromUrlAction` prompts for person name, platform, and URL/handle
- [ ] All actions display a Spectre.Console summary panel after ingestion: new posts count, duplicates skipped
- [ ] Person is created in SQLite if they don't exist

### US-010: Platform conventions
**Description:** As a developer, I need platform-specific rules (character limits, hashtag guidance, formatting) so that generated posts follow each platform's norms.

**Acceptance Criteria:**
- [ ] `PlatformConventions` class with a static lookup returning `PlatformRules` for each platform
- [ ] LinkedIn: 3,000 max chars, 1,200–1,800 recommended, 3–5 hashtags at end, sparingly emoji
- [ ] X/Twitter: 280 max chars (single tweet), 1–2 inline hashtags, moderate emoji
- [ ] Instagram: 2,200 max chars, 500–1,000 recommended, up to 30 hashtags in separate block, freely emoji
- [ ] Facebook: 63,206 max chars, 400–800 recommended, 1–3 hashtags or none, moderate emoji
- [ ] Unit tests verify rules for all four platforms

### US-011: Anthropic LLM provider
**Description:** As a user, I want to use Claude as the LLM backend so that I can analyse my style and generate posts using Anthropic's models.

**Acceptance Criteria:**
- [ ] `AnthropicProvider` implements `ILlmProvider`
- [ ] Uses the official `Anthropic` NuGet SDK if available, otherwise raw HTTP to `https://api.anthropic.com/v1/messages`
- [ ] API key read from `ANTHROPIC_API_KEY` environment variable
- [ ] Model configurable via `appsettings.json` (`Writegeist:Anthropic:Model`)
- [ ] Handles API errors gracefully with user-friendly messages
- [ ] Spinner displayed during API calls using Spectre.Console `AnsiConsole.Status()`

### US-012: OpenAI LLM provider
**Description:** As a user, I want to use OpenAI as an alternative LLM backend so that I have a choice of providers.

**Acceptance Criteria:**
- [ ] `OpenAiProvider` implements `ILlmProvider`
- [ ] Uses the official `OpenAI` NuGet package
- [ ] API key read from `OPENAI_API_KEY` environment variable
- [ ] Model configurable via `appsettings.json` (`Writegeist:OpenAi:Model`)
- [ ] Handles API errors gracefully with user-friendly messages
- [ ] Spinner displayed during API calls

### US-013: Style analysis
**Description:** As a user, I want to analyse my ingested posts to build a style profile so that Writegeist can learn how I write.

**Acceptance Criteria:**
- [ ] `AnalyseAction` appears as "Analyse Style" in the MainMenu
- [ ] Prompts user to select a person from existing persons (Spectre.Console selection prompt)
- [ ] Prompts user to select LLM provider (defaults from config)
- [ ] `StyleAnalyser` service loads all raw posts for the person and sends to the LLM with the style analysis prompt
- [ ] Resulting style profile JSON is stored in SQLite with provider and model metadata
- [ ] Profile summary is displayed in a styled Spectre.Console panel after analysis
- [ ] Shows an error if the person has no ingested posts
- [ ] Spinner displayed while LLM processes

### US-014: Post generation
**Description:** As a user, I want to generate a new post in my style for a specific platform so that I can quickly create on-brand content.

**Acceptance Criteria:**
- [ ] `GenerateAction` appears as "Generate Post" in the MainMenu
- [ ] Prompts user to select a person (only persons with a style profile)
- [ ] Prompts user to select a target platform (selection prompt)
- [ ] Prompts user to enter topic/key points (multi-line text prompt)
- [ ] `PostGenerator` service combines style profile + platform conventions + topic into the generation prompt
- [ ] Generated draft is stored in SQLite as a `GeneratedDraft`
- [ ] Generated post is displayed in a styled Spectre.Console panel
- [ ] Shows an error if no style profile exists for the selected person
- [ ] Spinner displayed while LLM processes

### US-015: Draft refinement
**Description:** As a user, I want to refine the last generated draft with natural language feedback so that I can iterate towards the perfect post.

**Acceptance Criteria:**
- [ ] `RefineAction` appears as "Refine Last Draft" in the MainMenu
- [ ] Loads the most recent `GeneratedDraft` and displays it
- [ ] Prompts for feedback (text prompt)
- [ ] Sends refinement prompt to LLM with current draft + feedback + style profile
- [ ] Stores refined draft linked to the previous one (`parent_draft_id`)
- [ ] Displays the refined post in a styled panel
- [ ] Asks "Refine again?" — loops if yes, returns to menu if no (uses `RepeatableActionAsync`)
- [ ] Shows an error if no drafts exist yet
- [ ] Spinner displayed while LLM processes

### US-016: Profile viewing
**Description:** As a user, I want to view and list style profiles so that I can see what Writegeist has learned about my writing.

**Acceptance Criteria:**
- [ ] `ProfileMenu` appears as "Profiles" in the MainMenu
- [ ] Sub-menu contains: "Show Profile", "List All Profiles", "Back"
- [ ] `ShowProfileAction` prompts for person selection and displays the full style profile in a formatted Spectre.Console table
- [ ] `ListProfilesAction` displays a table of all persons with profiles, showing name, provider, model, and date
- [ ] Shows a message if no profiles exist

### US-017: X/Twitter API fetcher
**Description:** As a user, I want to fetch my recent tweets automatically so that I don't have to manually copy them.

**Acceptance Criteria:**
- [ ] `XTwitterFetcher` implements `IContentFetcher` for platform `X`
- [ ] Uses X API v2 `GET /2/users/{id}/tweets` endpoint
- [ ] Bearer token read from `X_BEARER_TOKEN` environment variable
- [ ] Fetches up to 100 recent tweets per request
- [ ] Handles 429 rate limit responses gracefully with a user-friendly message
- [ ] Falls back to manual input suggestion if no bearer token is configured

### US-018: Stub fetchers for unsupported platforms
**Description:** As a developer, I need stub fetchers for LinkedIn, Instagram, and Facebook that give clear guidance to use manual input instead.

**Acceptance Criteria:**
- [ ] `LinkedInFetcher`, `InstagramFetcher`, `FacebookFetcher` each implement `IContentFetcher`
- [ ] Each throws a user-friendly exception explaining that automated fetching is not available for this platform
- [ ] Exception message suggests using "From File" or "Interactive Paste" instead
- [ ] `IngestFromUrlAction` catches these exceptions and displays the message via Spectre.Console markup

### US-019: Polished UI
**Description:** As a user, I want a visually polished CLI experience with colours, panels, tables, and spinners so that the tool feels professional.

**Acceptance Criteria:**
- [ ] All LLM calls wrapped in `AnsiConsole.Status()` with spinner animation
- [ ] Generated posts displayed in `Panel` with a border and title
- [ ] Style profiles displayed in formatted `Table` with grouped sections
- [ ] Ingestion summaries displayed in coloured markup (green for new, yellow for duplicates)
- [ ] Error messages displayed in red markup
- [ ] Menu descriptions are helpful and concise

## Functional Requirements

- FR-1: The application must launch into an interactive menu using DevJonny.InteractiveCli
- FR-2: The system must store persons, posts, style profiles, and drafts in a local SQLite database
- FR-3: The system must deduplicate ingested posts by SHA-256 content hash per person
- FR-4: The system must support ingesting posts from a text file (separated by `---`) or interactive paste
- FR-5: The system must support ingesting posts via X API v2 when a bearer token is configured
- FR-6: The system must display a user-friendly message when automated fetching is unavailable for a platform
- FR-7: The system must analyse ingested posts via an LLM and produce a structured JSON style profile
- FR-8: The system must generate platform-specific posts using a style profile and platform conventions
- FR-9: The system must support iterative refinement of the most recent draft with natural language feedback
- FR-10: The system must support both Anthropic (Claude) and OpenAI as LLM providers, selectable per action
- FR-11: The system must read API keys from environment variables (`ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, `X_BEARER_TOKEN`)
- FR-12: The system must display spinners during LLM API calls and styled output for all results
- FR-13: The default LLM provider must be configurable via `appsettings.json`

## Non-Goals

- No web UI or API server — this is a local CLI tool only
- No direct posting to social platforms (no OAuth write access)
- No multi-tweet thread generation for X
- No image generation or caption writing for Instagram
- No headless browser scraping (Playwright) for LinkedIn/Instagram/Facebook
- No user authentication or multi-user access control
- No cloud storage — all data stays in local SQLite
- No automatic style drift detection or profile comparison
- No clipboard integration or copy-to-clipboard
- No scheduled or automated post generation

## Technical Considerations

- **DevJonny.InteractiveCli** provides the host builder, DI, Serilog logging, and Spectre.Console integration. Actions are auto-registered in DI by assembly scanning.
- **InteractiveCli supports .NET 10** — all projects target net10.0.
- LLM provider implementations should handle network failures and API errors without crashing the interactive session — catch exceptions and display errors, then return to the menu.
- The `Anthropic` NuGet package (official C# SDK) should be verified to exist. If not, implement raw HTTP calls to the Messages API.
- Style profile JSON schema should be well-defined in a C# model for serialisation/deserialisation, even though it's stored as raw JSON in SQLite.

## Success Metrics

- User can go from zero to generated post in under 5 minutes (ingest → analyse → generate)
- Generated posts are indistinguishable in style from the user's real posts (subjective, tested manually)
- Refinement loop produces noticeably improved output within 1–2 iterations
- Both LLM providers produce usable results with the same prompts
- The interactive CLI is intuitive enough to use without reading documentation

## Open Questions

- Should the style analysis prompt be tuneable/editable by the user, or fixed?
- Should there be a way to merge style profiles from multiple platforms into one?
- Should the tool support exporting generated posts to a file?
- What's the minimum number of posts needed for a reliable style analysis? Should we warn if too few?
