using InteractiveCLI;
using InteractiveCLI.Options;
using Microsoft.Extensions.Hosting;
using Writegeist.Cli.Menus;

var host = Host.CreateDefaultBuilder(args)
    .AddInteractiveCli()
    .Build()
    .UseInteractiveCli((EmptyOptions _) => new MainMenu(), args);

await host.RunAsync();
