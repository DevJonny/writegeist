using InteractiveCLI;
using InteractiveCLI.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Writegeist.Cli.Menus;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Services;
using Writegeist.Infrastructure.Fetchers;
using Writegeist.Infrastructure.LlmProviders;
using Writegeist.Cli;
using Writegeist.Infrastructure.Persistence;

// Load .env file before building configuration
EnvironmentPrompt.LoadEnvFile();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var host = Host.CreateDefaultBuilder(args)
    .AddInteractiveCli(configuration, services =>
    {
        // Database
        services.AddSingleton<SqliteDatabase>();

        // Repositories
        services.AddSingleton<IPersonRepository, SqlitePersonRepository>();
        services.AddSingleton<IPostRepository, SqlitePostRepository>();
        services.AddSingleton<IStyleProfileRepository, SqliteStyleProfileRepository>();
        services.AddSingleton<IDraftRepository, SqliteDraftRepository>();

        // Secrets
        services.AddSingleton<ISecretProvider, EnvironmentPrompt>();

        // LLM providers
        services.AddSingleton<AnthropicProvider>();
        services.AddSingleton<OpenAiProvider>();
        services.AddHttpClient();
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var defaultProvider = config["Writegeist:DefaultProvider"] ?? "anthropic";
            return defaultProvider.ToLowerInvariant() switch
            {
                "openai" => sp.GetRequiredService<OpenAiProvider>(),
                _ => sp.GetRequiredService<AnthropicProvider>()
            };
        });

        // Fetchers
        services.AddSingleton<IContentFetcher, LinkedInFetcher>();
        services.AddSingleton<IContentFetcher, InstagramFetcher>();
        services.AddSingleton<IContentFetcher, FacebookFetcher>();
        services.AddSingleton<IContentFetcher, XTwitterFetcher>();

        // Services
        services.AddSingleton<StyleAnalyser>();
        services.AddSingleton<PostGenerator>();
    })
    .Build();

// Ensure database is created
var db = InteractiveCliBootstrapper.ServiceProvider.GetRequiredService<SqliteDatabase>();
db.EnsureCreated();

host.UseInteractiveCli((EmptyOptions _) => new MainMenu(), args);

await host.RunAsync();
