using InteractiveCLI.Actions;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;
using Writegeist.Core.Services;
using Writegeist.Infrastructure.LlmProviders;

namespace Writegeist.Cli.Actions;

public class GenerateAction(
    IPersonRepository personRepository,
    IStyleProfileRepository styleProfileRepository,
    IDraftRepository draftRepository,
    AnthropicProvider anthropicProvider,
    OpenAiProvider openAiProvider,
    IConfiguration configuration) : SingleActionAsync
{
    protected override async Task SingleAsyncAction()
    {
        try
        {
            var persons = await personRepository.GetAllAsync();
            if (persons.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No persons found. Ingest some posts first.[/]");
                return;
            }

            // Filter to only persons with a style profile
            var personsWithProfiles = new List<Person>();
            foreach (var person in persons)
            {
                var profile = await styleProfileRepository.GetLatestByPersonIdAsync(person.Id);
                if (profile is not null)
                    personsWithProfiles.Add(person);
            }

            if (personsWithProfiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No persons with style profiles found. Run 'Analyse Style' first.[/]");
                return;
            }

            var selectedPerson = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a person:")
                    .AddChoices(personsWithProfiles.Select(p => p.Name)));

            var person1 = personsWithProfiles.First(p => p.Name == selectedPerson);

            var selectedPlatform = AnsiConsole.Prompt(
                new SelectionPrompt<Platform>()
                    .Title("Select target platform:")
                    .AddChoices(Enum.GetValues<Platform>()));

            var topic = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter topic/key points:"));

            var defaultProvider = configuration["Writegeist:DefaultProvider"] ?? "anthropic";
            var providerChoices = new[] { "Anthropic", "OpenAI" };

            var selectedProvider = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select LLM provider:")
                    .AddChoices(providerChoices));

            ILlmProvider llmProvider = selectedProvider == "OpenAI"
                ? openAiProvider
                : anthropicProvider;

            var generator = new PostGenerator(styleProfileRepository, draftRepository, llmProvider);

            var draft = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Generating post...", async _ =>
                    await generator.GenerateAsync(person1.Id, selectedPlatform, topic));

            AnsiConsole.Write(new Panel(Markup.Escape(draft.Content))
                .Header($"Generated {selectedPlatform} Post")
                .Border(BoxBorder.Rounded));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
