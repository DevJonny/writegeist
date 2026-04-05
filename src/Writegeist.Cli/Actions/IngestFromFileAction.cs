using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;
using Writegeist.Infrastructure.Fetchers;

namespace Writegeist.Cli.Actions;

public class IngestFromFileAction(
    IPersonRepository personRepository,
    IPostRepository postRepository) : SingleActionAsync
{
    protected override async Task SingleAsyncAction()
    {
        var personName = AnsiConsole.Prompt(
            new TextPrompt<string>("Person name:"));

        var platform = AnsiConsole.Prompt(
            new SelectionPrompt<Platform>()
                .Title("Platform:")
                .AddChoices(Enum.GetValues<Platform>()));

        var filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("File path:"));

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine("[red]File not found.[/]");
            return;
        }

        var person = await personRepository.GetOrCreateAsync(personName);
        var fetcher = new ManualFetcher(platform);
        var request = new FetchRequest(FilePath: filePath);

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
                $"[green]Ingested {newCount} new post(s)[/] for [bold]{personName}[/] from {platform}" +
                (dupCount > 0 ? $" ([yellow]{dupCount} duplicate(s) skipped[/])" : ""))
                .Header("Ingest Complete"));
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
