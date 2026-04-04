using InteractiveCLI.Actions;
using Spectre.Console;

namespace Writegeist.Cli.Actions;

public class PlaceholderAction : SingleActionAsync
{
    protected override Task SingleAsyncAction()
    {
        AnsiConsole.MarkupLine("[grey]No actions configured yet.[/]");
        return Task.CompletedTask;
    }
}
