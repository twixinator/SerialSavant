// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0
using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SerialSavant.Config;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

#region Test Infrastructure

internal sealed class FakeLlamaServerGate(bool crashed = false) : ILlamaServerGate
{
    public Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (crashed)
            throw new InvalidOperationException("Server crashed");
        return Task.CompletedTask;
    }
}

internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request);
}

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler)
    {
        BaseAddress = new Uri("http://127.0.0.1:8080")
    };
}

#endregion

public sealed class LlamaCppAnalyzerTests
{
    private static readonly LlmConfig DefaultLlmConfig = new()
    {
        ModelPath = "test.gguf",
        ServerPort = 8080,
        MaxTokens = 512,
        Temperature = 0.7f
    };

    private static readonly LogEntry TestEntry = new(
        DateTimeOffset.UtcNow, "ERROR: ENOMEM - Cannot allocate memory", SerialLogLevel.Error);

    private static LlamaCppAnalyzer CreateAnalyzer(
        HttpMessageHandler handler,
        ILlamaServerGate? gate = null,
        LlmConfig? config = null)
    {
        var factory = new FakeHttpClientFactory(handler);
        return new LlamaCppAnalyzer(
            factory,
            gate ?? new FakeLlamaServerGate(),
            Options.Create(config ?? DefaultLlmConfig),
            NullLogger<LlamaCppAnalyzer>.Instance);
    }

    private static FakeHttpMessageHandler CreateHandler(string responseContent)
    {
        var responseJson = JsonSerializer.Serialize(
            new ChatCompletionResponse
            {
                Choices =
                [
                    new ChatChoice
                    {
                        Index = 0,
                        Message = new ChatMessage { Role = "assistant", Content = responseContent },
                        FinishReason = "stop"
                    }
                ]
            },
            LlamaCppJsonContext.Default.ChatCompletionResponse);

        return new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            }));
    }

    [Fact]
    public async Task Given_ValidJsonResponse_When_AnalyzeAsync_Then_ReturnsCorrectResult()
    {
        var llmOutput = """{"severity":"High","explanation":"Memory allocation failure","suggestions":["Check heap size","Review allocations"]}""";
        var handler = CreateHandler(llmOutput);
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);

        result.Severity.Should().Be(Severity.High);
        result.Explanation.Should().Be("Memory allocation failure");
        result.Suggestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_MalformedResponse_When_AnalyzeAsync_Then_ReturnsFallbackResult()
    {
        var handler = CreateHandler("I don't know how to respond in JSON, sorry!");
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);

        result.Severity.Should().Be(Severity.Medium);
        result.Explanation.Should().Contain("I don't know");
        result.Suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_HttpError_When_AnalyzeAsync_Then_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var analyzer = CreateAnalyzer(handler);

        var act = () => analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_CrashedServer_When_AnalyzeAsync_Then_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler("{}");
        var gate = new FakeLlamaServerGate(crashed: true);
        var analyzer = CreateAnalyzer(handler, gate);

        var act = () => analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Given_Cancellation_When_AnalyzeAsync_Then_ThrowsOperationCancelled()
    {
        var handler = CreateHandler("{}");
        var analyzer = CreateAnalyzer(handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => analyzer.AnalyzeAsync(TestEntry, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Given_JsonWrappedInProse_When_AnalyzeAsync_Then_ExtractsJsonCorrectly()
    {
        var llmOutput = """Sure! Here is my analysis: {"severity":"High","explanation":"OOM detected","suggestions":["Check heap size"]} Let me know if you need more.""";
        var handler = CreateHandler(llmOutput);
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);

        result.Severity.Should().Be(Severity.High);
        result.Explanation.Should().Be("OOM detected");
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task Given_InvalidSeverityString_When_AnalyzeAsync_Then_FallsBackToMedium(string severity)
    {
        var llmOutput = $$"""{"severity":"{{severity}}","explanation":"Something happened","suggestions":["Check it"]}""";
        var handler = CreateHandler(llmOutput);
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);

        result.Severity.Should().Be(Severity.Medium);
        result.Explanation.Should().Be("Something happened");
    }

    [Fact]
    public async Task Given_NullEntry_When_AnalyzeAsync_Then_ThrowsArgumentNullException()
    {
        var handler = CreateHandler("{}");
        var analyzer = CreateAnalyzer(handler);

        var act = () => analyzer.AnalyzeAsync(null!, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Given_EmptyExplanationAndSuggestions_When_AnalyzeAsync_Then_FallsBackToDefaults()
    {
        var llmOutput = """{"severity":"High","explanation":"","suggestions":[]}""";
        var handler = CreateHandler(llmOutput);
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(TestEntry, TestContext.Current.CancellationToken);

        result.Severity.Should().Be(Severity.High);
        result.Explanation.Should().NotBeNullOrWhiteSpace();
        result.Suggestions.Should().NotBeEmpty();
    }
}
