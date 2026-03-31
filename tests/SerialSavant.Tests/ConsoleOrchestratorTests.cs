// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SerialSavant.Core;
using SerialSavant.UI;

namespace SerialSavant.Tests;

public sealed class ConsoleOrchestratorTests
{
    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_FiniteStream_RendersAllEntriesAndSummaryAndStopsHost()
    {
        var entries = new[]
        {
            new LogEntry(DateTimeOffset.UtcNow, "line1", SerialLogLevel.Info),
            new LogEntry(DateTimeOffset.UtcNow, "line2", SerialLogLevel.Error),
        };
        var renderer = new FakeLogRenderer();
        var lifetime = new FakeApplicationLifetime();

        var orchestrator = Build(new FakeSerialReader(entries), new FakeAnalyzer(), renderer, lifetime);
        await orchestrator.StartAsync(TestContext.Current.CancellationToken);
        await lifetime.Stopped.WaitAsync(TestContext.Current.CancellationToken);

        renderer.RenderedCount.Should().Be(2);
        renderer.LastSummaryCount.Should().Be(2);
        renderer.HeaderWasCalled.Should().BeTrue();
        lifetime.StopWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_EmptyStream_RendersHeaderAndSummaryWithZeroCountAndStopsHost()
    {
        var renderer = new FakeLogRenderer();
        var lifetime = new FakeApplicationLifetime();

        var orchestrator = Build(new FakeSerialReader([]), new FakeAnalyzer(), renderer, lifetime);
        await orchestrator.StartAsync(TestContext.Current.CancellationToken);
        await lifetime.Stopped.WaitAsync(TestContext.Current.CancellationToken);

        renderer.HeaderWasCalled.Should().BeTrue();
        renderer.RenderedCount.Should().Be(0);
        renderer.LastSummaryCount.Should().Be(0);
        lifetime.StopWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AnalyzerAlwaysThrows_LogsErrorsAndCompletesSessionWithZeroRendered()
    {
        var entries = new[]
        {
            new LogEntry(DateTimeOffset.UtcNow, "bad-line", SerialLogLevel.Unknown),
        };
        var renderer = new FakeLogRenderer();
        var lifetime = new FakeApplicationLifetime();

        var orchestrator = Build(new FakeSerialReader(entries), new ThrowingAnalyzer(), renderer, lifetime);
        await orchestrator.StartAsync(TestContext.Current.CancellationToken);
        await lifetime.Stopped.WaitAsync(TestContext.Current.CancellationToken);

        renderer.RenderedCount.Should().Be(0);
        renderer.LastSummaryCount.Should().Be(0);  // session completed, no entries rendered
        lifetime.StopWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_WhileInfiniteStreamRunning_StopsCleanlyWithoutSummary()
    {
        var renderer = new FakeLogRenderer();
        var lifetime = new FakeApplicationLifetime();

        var orchestrator = Build(new FakeSerialReader(InfiniteEntries()), new FakeAnalyzer(), renderer, lifetime);
        await orchestrator.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);

        var act = async () => await orchestrator.StopAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        renderer.LastSummaryCount.Should().BeNull();  // cancelled — summary not rendered
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IEnumerable<LogEntry> InfiniteEntries()
    {
        while (true)
            yield return new LogEntry(DateTimeOffset.UtcNow, "x", SerialLogLevel.Debug);
    }

    private static ConsoleOrchestrator Build(
        ISerialReader reader,
        ILlmAnalyzer analyzer,
        FakeLogRenderer renderer,
        FakeApplicationLifetime lifetime) =>
        new(reader, analyzer, renderer, lifetime, NullLogger<ConsoleOrchestrator>.Instance);

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed class FakeSerialReader(IEnumerable<LogEntry> entries) : ISerialReader
    {
        public async IAsyncEnumerable<LogEntry> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry;
                await Task.Yield();
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeAnalyzer : ILlmAnalyzer
    {
        private static readonly AnalysisResult Fixed = AnalysisResult.Create(
            "fake explanation", Severity.Low, ["suggestion"]);

        public Task<AnalysisResult> AnalyzeAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Fixed);
        }
    }

    private sealed class ThrowingAnalyzer : ILlmAnalyzer
    {
        public Task<AnalysisResult> AnalyzeAsync(LogEntry entry, CancellationToken cancellationToken = default) =>
            Task.FromException<AnalysisResult>(new InvalidOperationException("simulated analyzer fault"));
    }

    private sealed class FakeLogRenderer : ILogRenderer
    {
        public int RenderedCount { get; private set; }
        public int? LastSummaryCount { get; private set; }
        public bool HeaderWasCalled { get; private set; }

        public void RenderHeader() => HeaderWasCalled = true;
        public void Render(LogEntry entry, AnalysisResult result) => RenderedCount++;
        public void RenderSummary(int totalCount) => LastSummaryCount = totalCount;
    }

    private sealed class FakeApplicationLifetime : IHostApplicationLifetime
    {
        private readonly TaskCompletionSource _stopped = new();

        public bool StopWasCalled { get; private set; }
        public Task Stopped => _stopped.Task;

        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
            StopWasCalled = true;
            _stopped.TrySetResult();
        }
    }
}
