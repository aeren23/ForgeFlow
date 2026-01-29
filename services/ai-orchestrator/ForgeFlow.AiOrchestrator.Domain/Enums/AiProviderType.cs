namespace ForgeFlow.AiOrchestrator.Domain.Enums;

/// <summary>
/// Supported AI/LLM provider types
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// Google Gemini (default - large context window)
    /// </summary>
    Gemini = 0,

    /// <summary>
    /// Groq (Llama 3 - fast and free tier available)
    /// </summary>
    Groq = 1,

    /// <summary>
    /// OpenAI (GPT-4o)
    /// </summary>
    OpenAI = 2,

    /// <summary>
    /// Local Ollama instance (self-hosted)
    /// </summary>
    Ollama = 3
}
