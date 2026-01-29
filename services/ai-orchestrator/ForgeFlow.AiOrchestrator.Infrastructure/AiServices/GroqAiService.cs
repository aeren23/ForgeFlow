using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.AiOrchestrator.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForgeFlow.AiOrchestrator.Infrastructure.AiServices;

/// <summary>
/// Groq AI Service implementation (Llama 3, Mixtral)
/// Uses OpenAI-compatible REST API
/// </summary>
public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqAiService> _logger;

    public AiProviderType ProviderType => AiProviderType.Groq;
    public string ModelName => _options.Model;

    public GroqAiService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Providers.Groq;
        _logger = logger;
    }

    public async Task<AiResponse> GenerateContentAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var url = $"{_options.BaseUrl}/chat/completions";

            // Build OpenAI-compatible request body
            var requestBody = new OpenAiChatRequest
            {
                Model = _options.Model,
                Messages =
                [
                    new OpenAiMessage { Role = "system", Content = request.SystemPrompt },
                    new OpenAiMessage { Role = "user", Content = request.UserPrompt }
                ],
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse rate limit headers
            int? remainingRequests = null;
            int? remainingTokens = null;
            DateTime? resetTime = null;

            if (response.Headers.TryGetValues("x-ratelimit-remaining-requests", out var reqValues))
                remainingRequests = int.TryParse(reqValues.FirstOrDefault(), out var r) ? r : null;

            if (response.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var tokValues))
                remainingTokens = int.TryParse(tokValues.FirstOrDefault(), out var t) ? t : null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Groq API error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                var errorCode = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => "QUOTA_EXCEEDED",
                    System.Net.HttpStatusCode.Unauthorized => "UNAUTHORIZED",
                    System.Net.HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
                    _ => "API_ERROR"
                };

                return AiResponse.Failure(responseContent, errorCode, ProviderType);
            }

            var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent);
            var generatedText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            stopwatch.Stop();

            return new AiResponse
            {
                IsSuccess = true,
                Content = generatedText,
                Provider = ProviderType,
                ModelName = ModelName,
                PromptTokens = chatResponse?.Usage?.PromptTokens ?? 0,
                CompletionTokens = chatResponse?.Usage?.CompletionTokens ?? 0,
                DurationMs = stopwatch.ElapsedMilliseconds,
                RemainingRequests = remainingRequests,
                RemainingTokens = remainingTokens,
                RateLimitResetTime = resetTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API call failed");
            stopwatch.Stop();

            return AiResponse.Failure(ex.Message, "EXCEPTION", ProviderType);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            return false;

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var url = $"{_options.BaseUrl}/models";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

#region OpenAI-Compatible API Models (Used by Groq)

internal class OpenAiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; } = [];

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

internal class OpenAiChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

internal class OpenAiUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

#endregion
