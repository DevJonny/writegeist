using InteractiveCLI.Menus;

namespace Writegeist.Cli.Menus;

public class MainMenu() : Menu(quitable: true, isTopLevel: true)
{
    protected override void BuildMenu()
    {
        MenuBuilder
            .AddMenuItem<IngestMenu>("Ingest Posts", "Import posts from files, URLs, or paste interactively");
    }
}
