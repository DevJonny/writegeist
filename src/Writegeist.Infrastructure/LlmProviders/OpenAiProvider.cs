using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Writegeist.Core.Interfaces;

namespace Writegeist.Infrastructure.LlmProviders;

public class OpenAiProvider : ILlmProvider
{
    private const string DefaultModel = "gpt-4o";

    private readonly ChatClient _chatClient;
    private readonly string _model;

    public OpenAiProvider(IConfiguration configuration)
    {
        var apiKey = configuration["OPENAI_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException(
                         "OpenAI API key is not configured. Set the OPENAI_API_KEY environment variable.");

        _model = configuration["Writegeist:OpenAi:Model"] ?? DefaultModel;
        _chatClient = new ChatClient(_model, apiKey);
    }

    public string ProviderName => "openai";
    public string ModelName => _model;

    public Task<string> AnalyseStyleAsync(string prompt) => SendMessageAsync(prompt);

    public Task<string> GeneratePostAsync(string prompt) => SendMessageAsync(prompt);

    public Task<string> RefinePostAsync(string prompt) => SendMessageAsync(prompt);

    private async Task<string> SendMessageAsync(string prompt)
    {
        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [new UserChatMessage(prompt)]);

            return completion.Value.Content[0].Text
                   ?? throw new InvalidOperationException("OpenAI API returned empty content.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var message = ex.Message switch
            {
                var m when m.Contains("401") => "Invalid OpenAI API key. Please check your OPENAI_API_KEY.",
                var m when m.Contains("429") => "OpenAI rate limit exceeded. Please wait a moment and try again.",
                var m when m.Contains("500") || m.Contains("502") || m.Contains("503") =>
                    "OpenAI API server error. Please try again later.",
                _ => $"OpenAI API request failed: {ex.Message}"
            };
            throw new HttpRequestException(message, ex);
        }
    }
}
