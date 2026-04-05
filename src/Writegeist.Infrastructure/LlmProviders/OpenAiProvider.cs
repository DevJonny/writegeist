using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Writegeist.Core.Interfaces;

namespace Writegeist.Infrastructure.LlmProviders;

public class OpenAiProvider : ILlmProvider
{
    private const string DefaultModel = "gpt-4o";

    private readonly ISecretProvider _secrets;
    private readonly string _model;
    private ChatClient? _chatClient;

    public OpenAiProvider(ISecretProvider secrets, IConfiguration configuration)
    {
        _secrets = secrets;
        _model = configuration["Writegeist:OpenAi:Model"] ?? DefaultModel;
    }

    private ChatClient GetClient()
    {
        if (_chatClient is not null)
            return _chatClient;

        var apiKey = _secrets.Require("OPENAI_API_KEY", "OpenAI API key");
        _chatClient = new ChatClient(_model, apiKey);
        return _chatClient;
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
            var completion = await GetClient().CompleteChatAsync(
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
