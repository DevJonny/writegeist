using InteractiveCLI.Actions;
using Spectre.Console;

namespace Writegeist.Cli.Actions;

public class IngestFromUrlAction : SingleActionAsync
{
    protected override Task SingleAsyncAction()
    {
        AnsiConsole.MarkupLine("[grey]URL/Handle ingestion — not yet implemented.[/]");
        return Task.CompletedTask;
    }
}
