using OpenAI;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using myAIApp.Config;


namespace myAIApp.Provider;

/// <summary>
/// Creates the correct IChatClient based on the "Provider:Selected" config value.
/// All providers unify behind IChatClient — callers never see provider details.
///
/// Supported: OpenAI | AzureOpenAI | Groq | Gemini | HuggingFace | Ollama
///
/// Groq, Gemini, and HuggingFace all expose OpenAI-compatible REST endpoints,
/// so we reuse OpenAIClient with a custom base URL — no custom HTTP client needed.
/// </summary>
public class ProviderFactory(
    IOptions<ProviderOptions>     providerOpts,
    IOptions<OpenAIOptions>       openAIOpts,
    IOptions<AzureOpenAIOptions>  azureOpts,
    IOptions<GroqOptions>         groqOpts,
    IOptions<GeminiOptions>       geminiOpts,
    IOptions<HuggingFaceOptions>  hfOpts,
    IOptions<OllamaOptions>       ollamaOpts)
{
    public IChatClient Create() => providerOpts.Value.Selected switch
    {
        "OpenAI"       => CreateOpenAI(),
        "AzureOpenAI"  => CreateAzureOpenAI(),
        "Groq"         => CreateGroq(),
        "Gemini"       => CreateGemini(),
        "HuggingFace"  => CreateHuggingFace(),
        "Ollama"       => CreateOllama(),
        var name       => throw new InvalidOperationException(
                              $"Unknown provider '{name}'. " +
                              $"Valid values: OpenAI, AzureOpenAI, Groq, Gemini, HuggingFace, Ollama")
    };

    // ── OpenAI ────────────────────────────────────────────────────────────
    private IChatClient CreateOpenAI()
    {
        var o = openAIOpts.Value;
        return new OpenAIClient(o.ApiKey)
            .GetChatClient(o.Model)
            .AsIChatClient();
    }

    // ── Azure OpenAI ──────────────────────────────────────────────────────
    private IChatClient CreateAzureOpenAI()
    {
        var o = azureOpts.Value;
        return new AzureOpenAIClient(
                new Uri(o.Endpoint),
                new AzureKeyCredential(o.ApiKey))
            .GetChatClient(o.Deployment)
            .AsIChatClient();
    }

    // ── Groq (OpenAI-compatible endpoint) ─────────────────────────────────
    // Groq's API is 100% OpenAI wire-compatible — just point the client at
    // their base URL. No custom implementation needed.
    private IChatClient CreateGroq()
    {
        var o = groqOpts.Value;
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(o.Endpoint)
        };
        return new OpenAIClient(new System.ClientModel.ApiKeyCredential(o.ApiKey), options)
            .GetChatClient(o.Model)
            .AsIChatClient();
    }

    // ── Gemini (OpenAI-compatible endpoint) ───────────────────────────────
    // Google exposes an OpenAI-compatible endpoint at:
    //   https://generativelanguage.googleapis.com/v1beta/openai
    // No custom GeminiChatClient needed — just redirect the OpenAI client.
    private IChatClient CreateGemini()
    {
        var o = geminiOpts.Value;
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(o.Endpoint)
        };
        return new OpenAIClient(new System.ClientModel.ApiKeyCredential(o.ApiKey), options)
            .GetChatClient(o.Model)
            .AsIChatClient();
    }

    // ── HuggingFace Inference API (OpenAI-compatible) ─────────────────────
    // HF's serverless inference API also exposes an OpenAI-compatible endpoint.
    // Works for hosted models like Phi-3, Mistral, Llama, etc.
    // For custom/private endpoints change the base URL accordingly.
    private IChatClient CreateHuggingFace()
    {
        var o = hfOpts.Value;
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(o.Endpoint)
        };
        return new OpenAIClient(new System.ClientModel.ApiKeyCredential(o.ApiKey), options)
            .GetChatClient(o.Model)
            .AsIChatClient();
    }

    // ── Ollama (local models) ─────────────────────────────────────────────
    // Microsoft.Extensions.AI ships a first-class OllamaChatClient.
    // Requires Ollama running locally: https://ollama.com
    private IChatClient CreateOllama()
    {
        var o = ollamaOpts.Value;
        return new OllamaApiClient(o.Endpoint, o.Model);
    }
}