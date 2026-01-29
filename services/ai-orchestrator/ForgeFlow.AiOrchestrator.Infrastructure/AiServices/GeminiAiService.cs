using System.Diagnostics;
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
/// Google Gemini AI Service implementation
/// Uses Google AI Studio REST API
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAiService> _logger;

    public AiProviderType ProviderType => AiProviderType.Gemini;
    public string ModelName => _options.Model;

    public GeminiAiService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Providers.Gemini;
        _logger = logger;
    }

    public async Task<AiResponse> GenerateContentAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build the request URL
            var url = $"{_options.BaseUrl}/models/{_options.Model}:generateContent?key={_options.ApiKey}";
            
            // Log the URL (masking the API key for security)
            var maskedUrl = url.Replace(_options.ApiKey, "***");
            _logger.LogInformation("Sending Gemini API request to: {Url}", maskedUrl);

            // Build the request body
            var requestBody = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts =
                        [
                            new GeminiPart { Text = $"{request.SystemPrompt}\n\n{request.UserPrompt}" }
                        ]
                    }
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = request.MaxTokens,
                    Temperature = request.Temperature
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                var errorCode = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => "QUOTA_EXCEEDED",
                    System.Net.HttpStatusCode.Unauthorized => "UNAUTHORIZED",
                    System.Net.HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
                    _ => "API_ERROR"
                };

                return AiResponse.Failure(responseContent, errorCode, ProviderType);
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            stopwatch.Stop();

            return new AiResponse
            {
                IsSuccess = true,
                Content = generatedText,
                Provider = ProviderType,
                ModelName = ModelName,
                PromptTokens = geminiResponse?.UsageMetadata?.PromptTokenCount ?? 0,
                CompletionTokens = geminiResponse?.UsageMetadata?.CandidatesTokenCount ?? 0,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
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
            var url = $"{_options.BaseUrl}/models?key={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Available Gemini Models: {Models}", content);
                return true;
            }
            
            _logger.LogWarning("Failed to list Gemini models: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Gemini availability");
            return false;
        }
    }
}

#region Gemini API Models

internal class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = [];

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

internal class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = [];
}

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

internal class GeminiGenerationConfig
{
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

internal class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}

#endregion
