using InteractiveCLI.Actions;
using Spectre.Console;

namespace Writegeist.Cli.Actions;

public class IngestInteractiveAction : RepeatableActionAsync
{
    protected override Task<bool> RepeatableAsyncAction()
    {
        AnsiConsole.MarkupLine("[grey]Interactive paste — not yet implemented.[/]");
        return Task.FromResult(true);
    }
}
