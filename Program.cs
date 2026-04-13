using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myAIApp.Agents;
using myAIApp.Extensions;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddDistributedMemoryCache();
services.AddAiProviders(config);

await using var sp = services.BuildServiceProvider();
var client = sp.GetRequiredService<IChatClient>();

Console.WriteLine($"Provider: {config["Provider:Selected"]}");
Console.WriteLine(new string('=', 50));

// ── 1. Single agent with memory ───────────────────────────────────────────
Console.WriteLine("\n[1] Single agent with memory");
Console.WriteLine(new string('-', 40));

var supportAgent = new AgentBuilder("Support", client)
    .WithSystemPrompt(
        "You are a friendly customer support agent for a software company. " +
        "Keep replies concise — 2 sentences max.")
    .WithTemperature(0.5f)
    .Build();

var r1 = await supportAgent.RunAsync("My app crashes on startup.");
Console.WriteLine($"Turn 1: {r1.Output}");

var r2 = await supportAgent.RunAsync("I am on Windows 11, .NET 10.");
Console.WriteLine($"Turn 2: {r2.Output}");
Console.WriteLine($"Tokens: {r1.InputTokens + r2.InputTokens} in / {r1.OutputTokens + r2.OutputTokens} out");

// ── 2. Single agent with tool ─────────────────────────────────────────────
Console.WriteLine("\n[2] Single agent with tool");
Console.WriteLine(new string('-', 40));

var getWeatherTool = AIFunctionFactory.Create(
    ([System.ComponentModel.Description("City name")] string city) =>
        $"Weather in {city}: 28C, sunny, humidity 60%",
    name: "get_weather",
    description: "Get current weather for a city"
);

var weatherAgent = new AgentBuilder("WeatherBot", client)
    .WithSystemPrompt("You answer weather questions. Use get_weather tool to get real data.")
    .WithTool(getWeatherTool)
    .WithTemperature(0.2f)
    .Build();

var weatherResult = await weatherAgent.RunAsync("What is the weather like in Tokyo?");
Console.WriteLine($"Weather: {weatherResult.Output}");

// ── 3. Sequential pipeline ────────────────────────────────────────────────
Console.WriteLine("\n[3] Sequential: Research -> Write -> Review");
Console.WriteLine(new string('-', 40));

var researcher = new AgentBuilder("Researcher", client)
    .WithSystemPrompt("You are a research assistant. Given a topic, list 3 key facts. Output only bullet list.")
    .WithTemperature(0.3f).Build();

var writer = new AgentBuilder("Writer", client)
    .WithSystemPrompt("You are a technical writer. Given research facts, write a 2-sentence blog intro. No bullets.")
    .WithTemperature(0.7f).Build();

var reviewer = new AgentBuilder("Reviewer", client)
    .WithSystemPrompt("You are an editor. Improve grammar and flow. Output only the final 2-sentence version.")
    .WithTemperature(0.4f).Build();

var seqPipeline = new AgentPipeline("BlogWriter")
    .AddAgent(researcher).AddAgent(writer).AddAgent(reviewer);

var seqResult = await seqPipeline.RunSequentialAsync("Benefits of dependency injection in .NET");

Console.WriteLine("Steps:");
foreach (var step in seqResult.Steps)
    Console.WriteLine($"  [{step.AgentName}] {step.Output[..Math.Min(80, step.Output.Length)]}...");
Console.WriteLine($"\nFinal:\n{seqResult.FinalOutput}");
Console.WriteLine($"Tokens: {seqResult.TotalInputTokens} in / {seqResult.TotalOutputTokens} out | {seqResult.TotalDuration.TotalSeconds:F1}s");

// ── 4. Parallel pipeline ──────────────────────────────────────────────────
Console.WriteLine("\n[4] Parallel: 3 analysts + merger");
Console.WriteLine(new string('-', 40));

var techAnalyst = new AgentBuilder("TechAnalyst", client)
    .WithSystemPrompt("Analyse technical feasibility only. One sentence.").WithTemperature(0.3f).Build();

var bizAnalyst = new AgentBuilder("BizAnalyst", client)
    .WithSystemPrompt("Analyse business value only. One sentence.").WithTemperature(0.3f).Build();

var riskAnalyst = new AgentBuilder("RiskAnalyst", client)
    .WithSystemPrompt("Analyse risks and downsides only. One sentence.").WithTemperature(0.3f).Build();

var merger = new AgentBuilder("Merger", client)
    .WithSystemPrompt("Synthesize multiple analyst opinions into one balanced recommendation. Two sentences max.")
    .WithTemperature(0.5f).Build();

var parallelPipeline = new AgentPipeline("AnalysisBoard")
    .AddAgent(techAnalyst).AddAgent(bizAnalyst).AddAgent(riskAnalyst);

var parResult = await parallelPipeline.RunParallelAsync(
    input: "Should we migrate our monolith to microservices?",
    mergerAgent: merger
);

Console.WriteLine("Opinions:");
foreach (var step in parResult.Steps.SkipLast(1))
    Console.WriteLine($"  [{step.AgentName}] {step.Output}");
Console.WriteLine($"\nSynthesized: {parResult.FinalOutput}");

// ── 5. LLM-based router ───────────────────────────────────────────────────
Console.WriteLine("\n[5] LLM router: orchestrator picks specialist");
Console.WriteLine(new string('-', 40));

var orchestrator = new AgentBuilder("Orchestrator", client)
    .WithSystemPrompt("You route requests to the correct specialist agent.")
    .WithTemperature(0.1f).Build();

var billingAgent    = new AgentBuilder("BillingAgent",     client).WithSystemPrompt("Handle billing and payment questions. Be brief.").Build();
var techSupportAgt  = new AgentBuilder("TechSupportAgent", client).WithSystemPrompt("Handle technical support questions. Be brief.").Build();
var salesAgent      = new AgentBuilder("SalesAgent",       client).WithSystemPrompt("Handle sales and pricing questions. Be brief.").Build();

var routerPipeline = new AgentPipeline("SupportRouter")
    .AddAgent(billingAgent).AddAgent(techSupportAgt).AddAgent(salesAgent);

var agentDescriptions = new Dictionary<string, string>
{
    ["BillingAgent"]     = "Handles invoices, payments, refunds, subscription questions",
    ["TechSupportAgent"] = "Handles bugs, errors, installation, technical issues",
    ["SalesAgent"]       = "Handles pricing, plans, upgrades, product comparisons"
};

var routedResult = await routerPipeline.RunLlmRoutedAsync(
    input: "My invoice shows the wrong amount this month.",
    orchestrator: orchestrator,
    agentDescriptions: agentDescriptions
);

Console.WriteLine($"Routed to:  {routedResult.Steps[0].Output.Trim()}");
Console.WriteLine($"Response:   {routedResult.FinalOutput}");

Console.WriteLine("\nAll done!");