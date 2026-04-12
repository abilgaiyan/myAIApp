using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using myAIApp.Config;
using myAIApp.Provider;

namespace myAIApp.Extensions;

public static class AiServiceExtensions
{
    /// <summary>
    /// Call this from Program.cs:
    ///   builder.Services.AddAiProviders(builder.Configuration);
    ///
    /// Registers:
    ///  - All strongly-typed provider options from appsettings.json
    ///  - ProviderFactory (injectable anywhere)
    ///  - IChatClient with the full MEAI middleware pipeline
    /// </summary>
    public static IServiceCollection AddAiProviders(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Bind all provider option sections ─────────────────────────────
        services.Configure<ProviderOptions>    (config.GetSection(ProviderOptions.Section));
        services.Configure<OpenAIOptions>      (config.GetSection(OpenAIOptions.Section));
        services.Configure<AzureOpenAIOptions> (config.GetSection(AzureOpenAIOptions.Section));
        services.Configure<GroqOptions>        (config.GetSection(GroqOptions.Section));
        services.Configure<GeminiOptions>      (config.GetSection(GeminiOptions.Section));
        services.Configure<HuggingFaceOptions> (config.GetSection(HuggingFaceOptions.Section));
        services.Configure<OllamaOptions>      (config.GetSection(OllamaOptions.Section));

        // ── Register the factory itself ────────────────────────────────────
        services.AddSingleton<ProviderFactory>();

        // ── Register IChatClient via factory + MEAI middleware pipeline ────
        // The pipeline wraps the raw provider client — any provider goes through
        // the same caching / logging / telemetry stack automatically.
        services.AddChatClient(sp =>
                sp.GetRequiredService<ProviderFactory>().Create()
            )
            .UseDistributedCache()     // semantic response caching (add IDistributedCache)
            .UseLogging()              // logs every request + response token counts
            .UseOpenTelemetry()        // OTEL traces with model/token attributes
            .UseFunctionInvocation();  // auto-invokes SK/MEAI tools the model calls

        return services;
    }
}