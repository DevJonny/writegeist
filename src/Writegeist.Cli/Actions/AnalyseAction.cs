using InteractiveCLI.Actions;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Services;
using Writegeist.Infrastructure.LlmProviders;

namespace Writegeist.Cli.Actions;

public class AnalyseAction(
    IPersonRepository personRepository,
    IPostRepository postRepository,
    IStyleProfileRepository styleProfileRepository,
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

            var selectedPerson = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a person:")
                    .AddChoices(persons.Select(p => p.Name)));

            var person = persons.First(p => p.Name == selectedPerson);

            var defaultProvider = configuration["Writegeist:DefaultProvider"] ?? "anthropic";
            var providerChoices = new[] { "Anthropic", "OpenAI" };
            var defaultChoice = defaultProvider.Equals("openai", StringComparison.OrdinalIgnoreCase)
                ? "OpenAI"
                : "Anthropic";

            var selectedProvider = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select LLM provider:")
                    .AddChoices(providerChoices));

            ILlmProvider llmProvider = selectedProvider == "OpenAI"
                ? openAiProvider
                : anthropicProvider;

            var analyser = new StyleAnalyser(postRepository, styleProfileRepository, llmProvider);

            var profile = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Analysing style...", async _ =>
                    await analyser.AnalyseAsync(person.Id));

            AnsiConsole.Write(new Panel(profile.ProfileJson)
                .Header($"Style Profile for {person.Name}")
                .Border(BoxBorder.Rounded));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
