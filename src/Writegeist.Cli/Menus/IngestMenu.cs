using InteractiveCLI.Menus;
using Writegeist.Cli.Actions;

namespace Writegeist.Cli.Menus;

public class IngestMenu() : Menu(quitable: false, isTopLevel: false)
{
    protected override void BuildMenu()
    {
        MenuBuilder
            .AddMenuItem<IngestFromFileAction>("From File", "Import posts from a text file (separated by ---)")
            .AddMenuItem<IngestInteractiveAction>("Interactive Paste", "Paste posts one at a time")
            .AddMenuItem<IngestFromUrlAction>("From URL / Handle", "Fetch posts from a social media URL or handle");
    }
}
