// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

// SerialSavant - UART Log Analyzer with Local LLM
// Run (mock, infinite):  dotnet run -- --mock
// Run (menu, 10 entries): dotnet run
// Publish (AOT):         dotnet publish -r win-x64 -c Release

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SerialSavant.Config;
using SerialSavant.Core;
using SerialSavant.Infrastructure;
using SerialSavant.UI;
using Spectre.Console;

// ── 1. Parse --mock flag ──────────────────────────────────────────────────────

const string APP_VERSION = "v0.1.0";

var isMockFlagSet = args.Contains("--mock", StringComparer.OrdinalIgnoreCase);

if (isMockFlagSet)
{
    AnsiConsole.MarkupLine(
        "[yellow][[WARNING]][/] Mock mode active: fabricated data only. Do not use in production.");
}

// ── 2. Interactive menu (skipped in --mock mode) ──────────────────────────────

// Collected for display; wiring to config is deferred to issue #9.
string? menuPort = null;
string? menuBaudRate = null;
string? menuModelPath = null;

if (!isMockFlagSet)
{
    AnsiConsole.MarkupLine($"[bold]SerialSavant[/] {APP_VERSION} — UART Log Analyzer with Local LLM");
    AnsiConsole.WriteLine();

    menuPort = AnsiConsole.Prompt(
        new TextPrompt<string>("Serial [grey]port[/] [dim](e.g. COM3 or /dev/ttyUSB0)[/]:")
            .DefaultValue("MOCK")
            .Validate(p =>
                !string.IsNullOrWhiteSpace(p)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Port must not be empty.[/]")));

    var baudRateChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Baud [grey]rate[/]:")
            .AddChoices(["9600", "115200", "921600"]));
    menuBaudRate = baudRateChoice;

    menuModelPath = AnsiConsole.Prompt(
        new TextPrompt<string>("LLM model [grey]path[/] [dim](GGUF file)[/]:")
            .DefaultValue("mock")
            .Validate(p =>
                !string.IsNullOrWhiteSpace(p)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Model path must not be empty.[/]")));

    AnsiConsole.WriteLine();
}

// ── 3. Build host ─────────────────────────────────────────────────────────────

var builder = Host.CreateApplicationBuilder(args);

// Config binding — EnableConfigurationBindingGenerator=true generates AOT-safe
// binding code at compile time; IL2026/IL3050 are suppressed because the source
// generator has already eliminated the reflection path.
// Menu values collected above are stored for future use (issue #9 will persist them).
#pragma warning disable IL2026, IL3050
builder.Services.Configure<SerialConfig>(builder.Configuration.GetSection("Serial"));
builder.Services.Configure<LlmConfig>(builder.Configuration.GetSection("Llm"));
#pragma warning restore IL2026, IL3050

builder.Services.AddOptions<SerialConfig>().ValidateOnStart();
builder.Services.AddOptions<LlmConfig>().ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<SerialConfig>, SerialConfigValidator>();
builder.Services.AddSingleton<IValidateOptions<LlmConfig>, LlmConfigValidator>();

// DI registrations.
builder.Services.AddSingleton<ILogRenderer, AnsiConsoleRenderer>();

if (isMockFlagSet)
{
    builder.Services.AddSingleton(new MockOptions { Mode = MockMode.Random });
    builder.Services.AddSingleton<ILlmAnalyzer, MockLlmAnalyzer>();
    builder.Services.AddSingleton<ISerialReader>(sp =>
    {
        var opts = sp.GetRequiredService<MockOptions>();
        var log = sp.GetRequiredService<ILogger<MockSerialReader>>();
        const int MOCK_ENTRY_DELAY_MS = 100;
        return new MockSerialReader(opts.Mode, delay: TimeSpan.FromMilliseconds(MOCK_ENTRY_DELAY_MS), logger: log);
    });
}
else
{
    // Serial reader (issue #4)
    builder.Services.AddSingleton<ISerialPortFactory, SystemSerialPortFactory>();
    builder.Services.AddSingleton<ISerialReader>(sp =>
    {
        var factory = sp.GetRequiredService<ISerialPortFactory>();
        var serialConfig = sp.GetRequiredService<IOptions<SerialConfig>>().Value;
        var log = sp.GetRequiredService<ILogger<UartSerialReader>>();
        return new UartSerialReader(factory, serialConfig, log);
    });

    // LLM analyzer (issue #5)
    builder.Services.AddSingleton<LlamaServerProcess>();
    builder.Services.AddSingleton<ILlamaServerGate>(sp => sp.GetRequiredService<LlamaServerProcess>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<LlamaServerProcess>());
    builder.Services.AddHttpClient("llama", (sp, client) =>
    {
        var llmOpts = sp.GetRequiredService<IOptions<LlmConfig>>().Value;
        client.BaseAddress = new Uri($"http://127.0.0.1:{llmOpts.ServerPort}");
        client.Timeout = TimeSpan.FromMilliseconds(llmOpts.TimeoutMs);
    });
    builder.Services.AddSingleton<ILlmAnalyzer, LlamaCppAnalyzer>();
}

builder.Services.AddHostedService<ConsoleOrchestrator>();

// ── 4. Run ────────────────────────────────────────────────────────────────────

var host = builder.Build();
await host.RunAsync();
