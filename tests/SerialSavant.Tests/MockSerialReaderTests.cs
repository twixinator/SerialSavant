// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class MockSerialReaderTests
{
    [Fact]
    public async Task ReadAsync_DeterministicMode_ReturnsExpectedSequence()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().NotBeEmpty();
        entries.Should().HaveCount(10);
    }

    [Fact]
    public async Task ReadAsync_DeterministicMode_AllLogCategoriesPresent()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        var rawLines = entries.Select(e => e.RawLine).ToList();

        // Hex dump: contains "0x" byte patterns
        rawLines.Should().Contain(line => line.Contains("0x", StringComparison.Ordinal));

        // C errno: contains known errno names
        rawLines.Should().Contain(line =>
            line.Contains("ENOMEM", StringComparison.Ordinal) ||
            line.Contains("SIGSEGV", StringComparison.Ordinal) ||
            line.Contains("EACCES", StringComparison.Ordinal));

        // Stack trace: frame entries start with '#' followed by a frame number
        rawLines.Should().Contain(line =>
            line.Contains("#0", StringComparison.Ordinal) ||
            line.Contains("#1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadAsync_DeterministicMode_LogLevelsMatchExpectedSequence()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        var levels = entries.Select(e => e.LogLevel).ToList();
        levels.Should().ContainInOrder(
            SerialLogLevel.Debug,   // hex dump 1
            SerialLogLevel.Debug,   // hex dump 2
            SerialLogLevel.Info,    // hex dump 3 (ELF header)
            SerialLogLevel.Error,   // ENOMEM
            SerialLogLevel.Error,   // EACCES
            SerialLogLevel.Fatal,   // SIGSEGV
            SerialLogLevel.Fatal,   // SIGBUS
            SerialLogLevel.Error,   // stack frame #0
            SerialLogLevel.Error,   // stack frame #1
            SerialLogLevel.Warning  // stack frame #2
        );
    }

    [Fact]
    public async Task ReadAsync_DeterministicMode_CompletesAfterFullSequence()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var count = 0;

        await foreach (var _ in reader.ReadAsync(cts.Token))
        {
            count++;
        }

        // Enumeration completed naturally (not via cancellation timeout)
        cts.IsCancellationRequested.Should().BeFalse();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReadAsync_RandomMode_RespectsDelayBetweenEntries()
    {
        var delay = TimeSpan.FromMilliseconds(50);
        await using var reader = new MockSerialReader(
            MockMode.Random,
            delay: delay,
            random: new Random(42));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;

        await foreach (var _ in reader.ReadAsync(cts.Token))
        {
            count++;
            if (count >= 3)
            {
                break;
            }
        }

        stopwatch.Stop();

        // Delay fires after each yield. For 3 entries consumed before break:
        // delay after entry 1, delay after entry 2, break before delay after entry 3
        // → 2 × 50ms = ~100ms minimum.
        // Threshold is conservative to absorb CI scheduling jitter.
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(90);
    }

    [Fact]
    public async Task ReadAsync_CancellationRequested_StopsEmitting()
    {
        await using var reader = new MockSerialReader(
            MockMode.Random,
            random: new Random(42));

        using var cts = new CancellationTokenSource();
        var count = 0;

        var act = async () =>
        {
            await foreach (var _ in reader.ReadAsync(cts.Token))
            {
                count++;
                if (count >= 5)
                {
                    cts.Cancel();
                }
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ReadAsync_PreCancelledToken_StopsImmediately()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var entries = new List<LogEntry>();

        var act = async () =>
        {
            await foreach (var entry in reader.ReadAsync(cts.Token))
            {
                entries.Add(entry);
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_NoOp_DoesNotThrow()
    {
        var reader = new MockSerialReader(MockMode.Deterministic);

        var act = async () => await reader.DisposeAsync();

        await act.Should().NotThrowAsync();
    }
}
