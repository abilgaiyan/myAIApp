using Microsoft.Extensions.AI;

namespace myAIApp.Agents;

// ── Fluent builder — makes agent definition readable in Program.cs ────────
//
// Usage:
//   var agent = new AgentBuilder("Researcher", client)
//       .WithSystemPrompt("You are a research assistant...")
//       .WithTool(AIFunctionFactory.Create(MyMethod))
//       .WithTemperature(0.3f)
//       .Build();

public class AgentBuilder(string name, IChatClient client)
{
    private string                   _systemPrompt = $"You are a helpful assistant named {name}.";
    private readonly List<AIFunction> _tools        = [];
    private float                    _temperature  = 0.7f;

    public AgentBuilder WithSystemPrompt(string prompt)
    {
        _systemPrompt = prompt;
        return this;
    }

    public AgentBuilder WithTool(AIFunction tool)
    {
        _tools.Add(tool);
        return this;
    }

    public AgentBuilder WithTools(IEnumerable<AIFunction> tools)
    {
        _tools.AddRange(tools);
        return this;
    }

    public AgentBuilder WithTemperature(float temperature)
    {
        _temperature = temperature;
        return this;
    }

    public Agent Build() => new(
        name:         name,
        systemPrompt: _systemPrompt,
        client:       client,
        tools:        _tools.Count > 0 ? _tools : null,
        temperature:  _temperature
    );
}