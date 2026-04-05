using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Writegeist.Core.Interfaces;

namespace Writegeist.Infrastructure.LlmProviders;

public class AnthropicProvider : ILlmProvider
{
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string DefaultModel = "claude-sonnet-4-20250514";
    private const string ApiVersion = "2023-06-01";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _model;

    public AnthropicProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["ANTHROPIC_API_KEY"]
                  ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                  ?? throw new InvalidOperationException(
                      "Anthropic API key is not configured. Set the ANTHROPIC_API_KEY environment variable.");

        _model = configuration["Writegeist:Anthropic:Model"] ?? DefaultModel;
    }

    public string ProviderName => "anthropic";
    public string ModelName => _model;

    public Task<string> AnalyseStyleAsync(string prompt) => SendMessageAsync(prompt);

    public Task<string> GeneratePostAsync(string prompt) => SendMessageAsync(prompt);

    public Task<string> RefinePostAsync(string prompt) => SendMessageAsync(prompt);

    private async Task<string> SendMessageAsync(string prompt)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        using var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var message = statusCode switch
            {
                401 => "Invalid Anthropic API key. Please check your ANTHROPIC_API_KEY.",
                429 => "Anthropic rate limit exceeded. Please wait a moment and try again.",
                >= 500 => $"Anthropic API server error ({statusCode}). Please try again later.",
                _ => $"Anthropic API request failed with status {statusCode}: {responseBody}"
            };
            throw new HttpRequestException(message);
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content ?? throw new InvalidOperationException("Anthropic API returned empty content.");
    }
}
