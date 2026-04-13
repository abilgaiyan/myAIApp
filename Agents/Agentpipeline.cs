using Microsoft.Extensions.AI;

namespace myAIApp.Agents;

// ── Result from an entire pipeline run ───────────────────────────────────
public record PipelineResult(
    string               PipelineName,
    string               FinalOutput,
    List<AgentResult>    Steps,
    TimeSpan             TotalDuration
)
{
    public long TotalInputTokens  => Steps.Sum(s => s.InputTokens);
    public long TotalOutputTokens => Steps.Sum(s => s.OutputTokens);
}

// ── Multi-agent pipeline ──────────────────────────────────────────────────
// Three execution modes:
//
//   Sequential  — Agent A output → Agent B input → Agent C input → ...
//                 Each agent refines or transforms the previous result.
//
//   Parallel    — All agents receive the SAME input simultaneously.
//                 Results collected and optionally merged by a final agent.
//
//   Router      — A routing function inspects the input and picks ONE agent.
//                 Useful for intent-based dispatch (support / sales / tech).

public class AgentPipeline(string name)
{
    private readonly List<Agent> _agents = [];

    public string Name => name;

    public AgentPipeline AddAgent(Agent agent)
    {
        _agents.Add(agent);
        return this;
    }

    // ── Sequential: output of step N becomes input of step N+1 ───────────
    public async Task<PipelineResult> RunSequentialAsync(
        string           initialInput,
        CancellationToken ct = default)
    {
        var start   = DateTime.UtcNow;
        var steps   = new List<AgentResult>();
        var current = initialInput;

        foreach (var agent in _agents)
        {
            ct.ThrowIfCancellationRequested();
            var result = await agent.RunStatelessAsync(current, ct);
            steps.Add(result);
            current = result.Output;   // chain output → next input
        }

        return new PipelineResult(
            PipelineName  : Name,
            FinalOutput   : current,
            Steps         : steps,
            TotalDuration : DateTime.UtcNow - start
        );
    }

    // ── Parallel: all agents get the same input, results run concurrently ─
    // optionally pass a merger agent to synthesize all outputs into one
    public async Task<PipelineResult> RunParallelAsync(
        string            input,
        Agent?            mergerAgent = null,
        CancellationToken ct          = default)
    {
        var start = DateTime.UtcNow;

        // Fan out — all agents run at the same time
        var tasks = _agents.Select(a => a.RunStatelessAsync(input, ct));
        var steps = (await Task.WhenAll(tasks)).ToList();

        string finalOutput;

        if (mergerAgent is not null)
        {
            // Ask the merger to synthesize all agent outputs
            var combined = string.Join("\n\n", steps.Select(
                s => $"[{s.AgentName}]:\n{s.Output}"
            ));
            var mergePrompt = $"The following are outputs from multiple agents for the input: \"{input}\"\n\n{combined}\n\nSynthesize these into a single coherent response.";
            var mergeResult = await mergerAgent.RunStatelessAsync(mergePrompt, ct);
            steps.Add(mergeResult);
            finalOutput = mergeResult.Output;
        }
        else
        {
            // No merger — concatenate all outputs
            finalOutput = string.Join("\n\n---\n\n", steps.Select(
                s => $"[{s.AgentName}]:\n{s.Output}"
            ));
        }

        return new PipelineResult(
            PipelineName  : Name,
            FinalOutput   : finalOutput,
            Steps         : steps,
            TotalDuration : DateTime.UtcNow - start
        );
    }

    // ── Router: a function inspects input and picks which agent to use ────
    // routingFn receives the input string and returns the agent NAME to use.
    // If no matching agent found, falls back to first agent.
    public async Task<PipelineResult> RunRoutedAsync(
        string              input,
        Func<string, string> routingFn,
        CancellationToken   ct = default)
    {
        var start      = DateTime.UtcNow;
        var agentName  = routingFn(input);
        var agent      = _agents.FirstOrDefault(a => a.Name == agentName) ?? _agents.First();
        var result     = await agent.RunStatelessAsync(input, ct);

        return new PipelineResult(
            PipelineName  : Name,
            FinalOutput   : result.Output,
            Steps         : [result],
            TotalDuration : DateTime.UtcNow - start
        );
    }

    // ── LLM-based router: an orchestrator agent decides which agent to use ─
    // The orchestrator sees agent names + descriptions and picks one.
    public async Task<PipelineResult> RunLlmRoutedAsync(
        string            input,
        Agent             orchestrator,
        Dictionary<string, string> agentDescriptions,
        CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;
        var steps = new List<AgentResult>();

        // Build routing prompt
        var menu = string.Join("\n", agentDescriptions.Select(
            kv => $"- {kv.Key}: {kv.Value}"
        ));
        var routingPrompt =
            $"You must choose exactly ONE agent name from this list to handle the request.\n" +
            $"Reply with ONLY the agent name, nothing else.\n\n" +
            $"Available agents:\n{menu}\n\n" +
            $"Request: {input}";

        var routingResult = await orchestrator.RunStatelessAsync(routingPrompt, ct);
        steps.Add(routingResult);

        var chosen = routingResult.Output.Trim();
        var agent  = _agents.FirstOrDefault(a =>
                         string.Equals(a.Name, chosen, StringComparison.OrdinalIgnoreCase))
                     ?? _agents.First();

        var finalResult = await agent.RunStatelessAsync(input, ct);
        steps.Add(finalResult);

        return new PipelineResult(
            PipelineName  : Name,
            FinalOutput   : finalResult.Output,
            Steps         : steps,
            TotalDuration : DateTime.UtcNow - start
        );
    }
}