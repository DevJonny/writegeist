using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Cli.Actions;

public class IngestInteractiveAction(
    IPersonRepository personRepository,
    IPostRepository postRepository) : RepeatableActionAsync
{
    private Person? _person;
    private Platform _platform;
    private int _newCount;
    private int _dupCount;
    private bool _isFirstIteration = true;

    protected override async Task<bool> RepeatableAsyncAction()
    {
        if (_isFirstIteration)
        {
            var personName = AnsiConsole.Prompt(
                new TextPrompt<string>("Person name:"));

            _platform = AnsiConsole.Prompt(
                new SelectionPrompt<Platform>()
                    .Title("Platform:")
                    .AddChoices(Enum.GetValues<Platform>()));

            _person = await personRepository.GetOrCreateAsync(personName);
            _isFirstIteration = false;
        }

        var content = AnsiConsole.Prompt(
            new TextPrompt<string>("Paste post content:"));

        var rawPost = new RawPost
        {
            PersonId = _person!.Id,
            Platform = _platform,
            Content = content,
            FetchedAt = DateTime.UtcNow
        };

        var isNew = await postRepository.AddAsync(rawPost);
        if (isNew) _newCount++;
        else _dupCount++;

        var addAnother = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Add another post?")
                .AddChoices("Yes", "No"));

        if (addAnother == "No")
        {
            AnsiConsole.Write(new Panel(
                $"[green]Ingested {_newCount} new post(s)[/] for [bold]{Markup.Escape(_person.Name)}[/] from {_platform}" +
                (_dupCount > 0 ? $" ([yellow]{_dupCount} duplicate(s) skipped[/])" : ""))
                .Header("Ingest Complete"));
            return true;
        }

        return false;
    }
}
