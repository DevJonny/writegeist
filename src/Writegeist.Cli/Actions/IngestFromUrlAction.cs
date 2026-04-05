using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Cli.Actions;

public class IngestFromUrlAction(
    IPersonRepository personRepository,
    IPostRepository postRepository,
    IEnumerable<IContentFetcher> fetchers) : SingleActionAsync
{
    protected override async Task SingleAsyncAction()
    {
        var personName = AnsiConsole.Prompt(
            new TextPrompt<string>("Person name:"));

        var platform = AnsiConsole.Prompt(
            new SelectionPrompt<Platform>()
                .Title("Platform:")
                .AddChoices(Enum.GetValues<Platform>()));

        var urlOrHandle = AnsiConsole.Prompt(
            new TextPrompt<string>("URL or handle:"));

        var person = await personRepository.GetOrCreateAsync(personName);

        var fetcher = fetchers.FirstOrDefault(f => f.Platform == platform);
        if (fetcher is null)
        {
            AnsiConsole.MarkupLine($"[yellow]No fetcher available for {platform}.[/]");
            return;
        }

        var request = new FetchRequest(Url: urlOrHandle, Handle: urlOrHandle);

        try
        {
            var posts = await fetcher.FetchPostsAsync(request);
            var newCount = 0;
            var dupCount = 0;

            foreach (var fetchedPost in posts)
            {
                var rawPost = new RawPost
                {
                    PersonId = person.Id,
                    Platform = platform,
                    Content = fetchedPost.Content,
                    SourceUrl = fetchedPost.SourceUrl,
                    FetchedAt = DateTime.UtcNow
                };

                var isNew = await postRepository.AddAsync(rawPost);
                if (isNew) newCount++;
                else dupCount++;
            }

            AnsiConsole.Write(new Panel(
                $"[green]Ingested {newCount} new post(s)[/] for [bold]{Markup.Escape(personName)}[/] from {platform}" +
                (dupCount > 0 ? $" ([yellow]{dupCount} duplicate(s) skipped[/])" : ""))
                .Header("Ingest Complete"));
        }
        catch (NotSupportedException ex)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(ex.Message)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
