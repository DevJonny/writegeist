using InteractiveCLI.Menus;
using Writegeist.Cli.Actions;

namespace Writegeist.Cli.Menus;

public class ProfileMenu() : Menu(quitable: false, isTopLevel: false)
{
    protected override void BuildMenu()
    {
        MenuBuilder
            .AddMenuItem<ShowProfileAction>("Show Profile", "View the full style profile for a person")
            .AddMenuItem<ListProfilesAction>("List All Profiles", "List all persons with style profiles");
    }
}
