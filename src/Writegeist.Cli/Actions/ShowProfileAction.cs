using System.Text.Json;
using InteractiveCLI.Actions;
using Spectre.Console;
using Writegeist.Core.Interfaces;

namespace Writegeist.Cli.Actions;

public class ShowProfileAction(
    IPersonRepository personRepository,
    IStyleProfileRepository styleProfileRepository) : SingleActionAsync
{
    protected override async Task SingleAsyncAction()
    {
        try
        {
            var persons = await personRepository.GetAllAsync();
            if (persons.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No persons found. Ingest some posts first.[/]");
                return;
            }

            // Filter to persons with profiles
            var personsWithProfiles = new List<(string Name, int Id)>();
            foreach (var person in persons)
            {
                var profile = await styleProfileRepository.GetLatestByPersonIdAsync(person.Id);
                if (profile is not null)
                    personsWithProfiles.Add((person.Name, person.Id));
            }

            if (personsWithProfiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No style profiles found. Run 'Analyse Style' first.[/]");
                return;
            }

            var selectedName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a person:")
                    .AddChoices(personsWithProfiles.Select(p => p.Name)));

            var personId = personsWithProfiles.First(p => p.Name == selectedName).Id;
            var latestProfile = (await styleProfileRepository.GetLatestByPersonIdAsync(personId))!;

            // Try to parse and display as structured table
            try
            {
                var json = JsonDocument.Parse(latestProfile.ProfileJson);
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title($"Style Profile for {selectedName}")
                    .AddColumn("Section")
                    .AddColumn("Details");

                foreach (var property in json.RootElement.EnumerateObject())
                {
                    var value = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? ""
                        : property.Value.GetRawText();

                    table.AddRow(
                        Markup.Escape(property.Name),
                        Markup.Escape(value));
                }

                AnsiConsole.Write(table);
            }
            catch (JsonException)
            {
                // Not valid JSON — display as raw text in a panel
                AnsiConsole.Write(new Panel(Markup.Escape(latestProfile.ProfileJson))
                    .Header($"Style Profile for {selectedName}")
                    .Border(BoxBorder.Rounded));
            }

            AnsiConsole.MarkupLine($"[dim]Provider: {Markup.Escape(latestProfile.Provider)} | Model: {Markup.Escape(latestProfile.Model)} | Created: {latestProfile.CreatedAt:g}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
