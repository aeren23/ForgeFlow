namespace ForgeFlow.AiOrchestrator.Infrastructure.Options;

/// <summary>
/// Configuration options for AI providers
/// </summary>
public class AiOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// Default provider to use (Gemini, Groq, OpenAI)
    /// </summary>
    public string DefaultProvider { get; set; } = "Gemini";

    /// <summary>
    /// Provider-specific configurations
    /// </summary>
    public ProvidersOptions Providers { get; set; } = new();
}

public class ProvidersOptions
{
    public GeminiOptions Gemini { get; set; } = new();
    public GroqOptions Groq { get; set; } = new();
    public OpenAiOptions OpenAI { get; set; } = new();
}

public class GeminiOptions
{
    /// <summary>
    /// Google AI Studio API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name (e.g., gemini-1.5-pro, gemini-1.5-flash)
    /// </summary>
    public string Model { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// Base URL for Gemini API
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

public class GroqOptions
{
    /// <summary>
    /// Groq API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name (e.g., llama3-70b-8192, mixtral-8x7b-32768)
    /// </summary>
    public string Model { get; set; } = "llama3-70b-8192";

    /// <summary>
    /// Base URL for Groq API (OpenAI compatible)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public class OpenAiOptions
{
    /// <summary>
    /// OpenAI API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name (e.g., gpt-4o, gpt-4-turbo)
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Base URL for OpenAI API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
