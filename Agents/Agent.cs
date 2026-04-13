using Microsoft.Extensions.AI;

namespace myAIApp.Agents;

// ── What one "run" of an agent produces ──────────────────────────────────
public record AgentResult(
    string      AgentName,
    string      Output,
    long         InputTokens,
    long         OutputTokens,
    TimeSpan    Duration
);

// ── A single agent ────────────────────────────────────────────────────────
// Holds its own chat history (memory), a system prompt that defines its role,
// and an optional set of AIFunctions (tools) it can call.
// Everything runs through the IChatClient your ProviderFactory already built.
public class Agent
{
    private readonly IChatClient        _client;
    private readonly List<ChatMessage>  _history = [];
    private readonly ChatOptions?       _options;

    public string Name           { get; }
    public string SystemPrompt   { get; }

    public Agent(
        string       name,
        string       systemPrompt,
        IChatClient  client,
        IEnumerable<AIFunction>? tools = null,
        float        temperature = 0.7f)
    {
        Name         = name;
        SystemPrompt = systemPrompt;
        _client      = client;

        // Seed history with system prompt
        _history.Add(new ChatMessage(ChatRole.System, systemPrompt));

        // Build ChatOptions only if tools or temperature differ from default
        if (tools?.Any() == true || temperature != 0.7f)
        {
            _options = new ChatOptions
            {
                Temperature           = temperature,
                Tools                 = tools?.ToList<AITool>(),
                ToolMode              = tools?.Any() == true
                                            ? ChatToolMode.Auto
                                            : ChatToolMode.None
            };
        }
    }

    // ── Single turn — remembers history across calls ──────────────────────
    public async Task<AgentResult> RunAsync(string userMessage, CancellationToken ct = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, userMessage));

        var start    = DateTime.UtcNow;
        var response = await _client.GetResponseAsync(_history, _options, ct);
        var elapsed  = DateTime.UtcNow - start;

        _history.AddMessages(response);

        return new AgentResult(
            AgentName    : Name,
            Output       : response.Text ?? string.Empty,
            InputTokens  : response.Usage?.InputTokenCount  ?? 0,
            OutputTokens : response.Usage?.OutputTokenCount ?? 0,
            Duration     : elapsed
        );
    }

    // ── Stateless turn — ignores stored history, uses only system prompt ──
    // Useful when agents call each other and you don't want cross-contamination.
    public async Task<AgentResult> RunStatelessAsync(string userMessage, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   userMessage)
        };

        var start    = DateTime.UtcNow;
        var response = await _client.GetResponseAsync(messages, _options, ct);
        var elapsed  = DateTime.UtcNow - start;

        return new AgentResult(
            AgentName    : Name,
            Output       : response.Text ?? string.Empty,
            InputTokens  : response.Usage?.InputTokenCount  ?? 0,
            OutputTokens : response.Usage?.OutputTokenCount ?? 0,
            Duration     : elapsed
        );
    }

    // ── Streaming single turn ─────────────────────────────────────────────
    public async IAsyncEnumerable<string> StreamAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, userMessage));

        var fullText = new System.Text.StringBuilder();

        await foreach (var update in _client.GetStreamingResponseAsync(_history, _options, ct))
        {
            if (update.Text is { Length: > 0 } chunk)
            {
                fullText.Append(chunk);
                yield return chunk;
            }
        }

        // Commit assistant reply to history after streaming completes
        _history.Add(new ChatMessage(ChatRole.Assistant, fullText.ToString()));
    }

    // ── Reset memory (keep system prompt) ─────────────────────────────────
    public void ClearHistory()
    {
        _history.Clear();
        _history.Add(new ChatMessage(ChatRole.System, SystemPrompt));
    }

    public IReadOnlyList<ChatMessage> GetHistory() => _history.AsReadOnly();
}