using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;

namespace Writegeist.Cli.Actions;

public class ListProfilesAction(
    IPersonRepository personRepository,
    IStyleProfileRepository styleProfileRepository) : SingleActionAsync
{
    protected override async Task SingleAsyncAction()
    {
        try
        {
            var persons = await personRepository.GetAllAsync();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("Style Profiles")
                .AddColumn("Name")
                .AddColumn("Provider")
                .AddColumn("Model")
                .AddColumn("Created");

            var hasProfiles = false;

            foreach (var person in persons)
            {
                var profile = await styleProfileRepository.GetLatestByPersonIdAsync(person.Id);
                if (profile is not null)
                {
                    hasProfiles = true;
                    table.AddRow(
                        Markup.Escape(person.Name),
                        Markup.Escape(profile.Provider),
                        Markup.Escape(profile.Model),
                        profile.CreatedAt.ToString("g"));
                }
            }

            if (!hasProfiles)
            {
                AnsiConsole.MarkupLine("[yellow]No style profiles found. Run 'Analyse Style' first.[/]");
                return;
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
