using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;
using Writegeist.Core.Services;
using Writegeist.Infrastructure.LlmProviders;

namespace Writegeist.Cli.Actions;

public class RefineAction(
    IDraftRepository draftRepository,
    IStyleProfileRepository styleProfileRepository,
    AnthropicProvider anthropicProvider,
    OpenAiProvider openAiProvider) : RepeatableActionAsync
{
    private GeneratedDraft? _currentDraft;
    private PostGenerator? _generator;
    private bool _isFirstIteration = true;

    protected override async Task<bool> RepeatableAsyncAction()
    {
        try
        {
            if (_isFirstIteration)
            {
                _currentDraft = await draftRepository.GetLatestAsync();
                if (_currentDraft is null)
                {
                    AnsiConsole.MarkupLine("[red]No drafts found. Generate a post first.[/]");
                    return true;
                }

                AnsiConsole.Write(new Panel(Markup.Escape(_currentDraft.Content))
                    .Header($"Current Draft ({_currentDraft.Platform})")
                    .Border(BoxBorder.Rounded));

                var selectedProvider = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select LLM provider:")
                        .AddChoices("Anthropic", "OpenAI"));

                ILlmProvider llmProvider = selectedProvider == "OpenAI"
                    ? openAiProvider
                    : anthropicProvider;

                _generator = new PostGenerator(styleProfileRepository, draftRepository, llmProvider);
                _isFirstIteration = false;
            }

            var feedback = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter feedback:"));

            _currentDraft = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Refining post...", async _ =>
                    await _generator!.RefineAsync(_currentDraft!.Id, feedback));

            AnsiConsole.Write(new Panel(Markup.Escape(_currentDraft.Content))
                .Header($"Refined Draft ({_currentDraft.Platform})")
                .Border(BoxBorder.Rounded));

            var refineAgain = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Refine again?")
                    .AddChoices("Yes", "No"));

            return refineAgain == "No";
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return true;
        }
    }
}
