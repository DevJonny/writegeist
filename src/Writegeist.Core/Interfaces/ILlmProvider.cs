namespace Writegeist.Core.Interfaces;

public interface ILlmProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    Task<string> AnalyseStyleAsync(string prompt);
    Task<string> GeneratePostAsync(string prompt);
    Task<string> RefinePostAsync(string prompt);
}
