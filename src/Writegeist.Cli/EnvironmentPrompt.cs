using Spectre.Console;
using Writegeist.Core.Interfaces;

namespace Writegeist.Cli;

public class EnvironmentPrompt : ISecretProvider
{
    private const string EnvFileName = ".env";

    /// <summary>
    /// Returns the value for the given key from environment, or prompts the user
    /// to enter it and persists it to the .env file for future runs.
    /// </summary>
    public string Require(string key, string description)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(key)} is not set.[/]");
        value = AnsiConsole.Prompt(
            new TextPrompt<string>($"Enter your {Markup.Escape(description)}:")
                .Secret());

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is required but was not provided.");

        // Persist to .env file and set for current process
        Environment.SetEnvironmentVariable(key, value);
        PersistToEnvFile(key, value);

        AnsiConsole.MarkupLine($"[green]Saved {Markup.Escape(key)} to .env file.[/]");
        return value;
    }

    private static void PersistToEnvFile(string key, string value)
    {
        var envPath = Path.Combine(AppContext.BaseDirectory, EnvFileName);

        var lines = File.Exists(envPath)
            ? File.ReadAllLines(envPath).ToList()
            : [];

        var prefix = $"{key}=";
        var index = lines.FindIndex(l => l.StartsWith(prefix, StringComparison.Ordinal));

        var newLine = $"{key}={value}";
        if (index >= 0)
            lines[index] = newLine;
        else
            lines.Add(newLine);

        File.WriteAllLines(envPath, lines);
    }

    /// <summary>
    /// Loads variables from the .env file into the current process environment.
    /// Call this early in startup before configuration is built.
    /// </summary>
    public static void LoadEnvFile()
    {
        var envPath = Path.Combine(AppContext.BaseDirectory, EnvFileName);
        if (!File.Exists(envPath))
            return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var val = line[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, val);
        }
    }
}
