namespace myAIApp.Config;

// Strongly-typed options — bound from appsettings.json


public class ProviderOptions
{
    public const string Section = "Provider";
    public string Selected { get; set; } = "OpenAI";
}

public class OpenAIOptions
{
    public const string Section = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public class AzureOpenAIOptions
{
    public const string Section = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string Deployment { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class GroqOptions
{
    public const string Section = "Groq";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama3-70b-8192";
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1";
}

public class GeminiOptions
{
    public const string Section = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    // Gemini exposes an OpenAI-compatible endpoint — no custom client needed
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai";
}

public class HuggingFaceOptions
{
    public const string Section = "HuggingFace";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "microsoft/Phi-3-mini-4k-instruct";
    // HF Inference API also exposes an OpenAI-compatible endpoint
    public string Endpoint { get; set; } = "https://api-inference.huggingface.co/v1";
}

public class OllamaOptions
{
    public const string Section = "Ollama";
    public string Model { get; set; } = "llama3.2";
    public string Endpoint { get; set; } = "http://localhost:11434";
}