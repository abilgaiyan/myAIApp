using Microsoft.Extensions.AI;

namespace myAIApp.Extensions;

// ── Convenience extensions on IChatClient ─────────────────────────────────

public static class ChatClientExtensions
{
    /// <summary>Simple string → string shorthand.</summary>
    public static async Task<string> AskAsync(
        this IChatClient client,
        string prompt,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? string.Empty;
    }

    /// <summary>System prompt + user prompt shorthand.</summary>
    public static async Task<string> AskAsync(
        this IChatClient client,
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? string.Empty;
    }

    /// <summary>Streaming version — yields text chunks as they arrive.</summary>
    public static async IAsyncEnumerable<string> StreamAskAsync(
        this IChatClient client,
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        await foreach (var update in client.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            if (update.Text is { Length: > 0 } chunk)
                yield return chunk;
        }
    }
}