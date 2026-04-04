using InteractiveCLI.Menus;
using Writegeist.Cli.Actions;

namespace Writegeist.Cli.Menus;

public class MainMenu() : Menu(quitable: true, isTopLevel: true)
{
    protected override void BuildMenu()
    {
        MenuBuilder.AddMenuItem<PlaceholderAction>("Placeholder", "Placeholder action");
    }
}
