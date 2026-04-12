using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myAIApp.Extensions;

// ── Configuration ─────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// ── DI container ──────────────────────────────────────────────────────────
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddDistributedMemoryCache();
services.AddAiProviders(config);

await using var sp = services.BuildServiceProvider();

// ── Run ───────────────────────────────────────────────────────────────────
var chatClient = sp.GetRequiredService<IChatClient>();

Console.WriteLine($"Provider : {config["Provider:Selected"]}");
Console.WriteLine(new string('-', 40));

// 1. Simple one-shot
var answer = await chatClient.AskAsync("What is the capital of India? Reply in one word.");
Console.WriteLine($"One-shot : {answer}");

// 2. With system prompt
var formal = await chatClient.AskAsync(
    systemPrompt: "You are a concise assistant. One sentence only.",
    userPrompt:   "What is dependency injection?");
Console.WriteLine($"With sys : {formal}");

// 3. Streaming
Console.Write("Streaming: ");
await foreach (var chunk in chatClient.StreamAskAsync("Count 1 to 5, space-separated."))
    Console.Write(chunk);

Console.WriteLine("\n\nDone.");