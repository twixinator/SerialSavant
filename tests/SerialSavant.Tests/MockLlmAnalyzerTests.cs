// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class MockLlmAnalyzerTests
{
    private readonly MockLlmAnalyzer _analyzer = new();

    private static LogEntry MakeEntry(string rawLine) =>
        new(DateTimeOffset.UtcNow, rawLine, SerialLogLevel.Unknown);

    [Theory]
    [InlineData("0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD")]
    [InlineData("0xCA 0xFE 0xBA 0xBE 0x00 0x00 0x01")]
    [InlineData("0x7F 0x45 0x4C 0x46 0x02 0x01 0x01")]
    public async Task AnalyzeAsync_HexDumpInput_ReturnsLowSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.Low);
    }

    [Theory]
    [InlineData("ERROR: ENOMEM - Cannot allocate memory", Severity.High)]
    [InlineData("ERROR: EACCES - Permission denied", Severity.High)]
    [InlineData("FATAL: SIGSEGV - Segmentation fault at 0x00000010", Severity.Critical)]
    [InlineData("FATAL: SIGBUS - Bus error at 0x0000DEAD", Severity.Critical)]
    public async Task AnalyzeAsync_ErrnoInput_ReturnsHighOrCriticalSeverity(
        string rawLine, Severity expectedSeverity)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(expectedSeverity);
    }

    [Theory]
    [InlineData("#0 0x0800ABCD in main() at firmware.c:142")]
    [InlineData("#1 0x0800EF01 in init_hardware() at hal.c:87")]
    public async Task AnalyzeAsync_StackTraceInput_ReturnsHighSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.High);
    }

    [Theory]
    [InlineData("Some random log message")]
    [InlineData("INFO: System started")]
    [InlineData("")]
    public async Task AnalyzeAsync_UnknownInput_ReturnsMediumSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.Medium);
    }

    [Fact]
    public async Task AnalyzeAsync_SameInput_ReturnsDeterministicResult()
    {
        var entry = MakeEntry("ERROR: ENOMEM - Cannot allocate memory");

        var result1 = await _analyzer.AnalyzeAsync(entry);
        var result2 = await _analyzer.AnalyzeAsync(entry);

        result1.Explanation.Should().Be(result2.Explanation);
        result1.Severity.Should().Be(result2.Severity);
        result1.Suggestions.Should().BeEquivalentTo(result2.Suggestions);
    }

    [Theory]
    [InlineData("0x00 0x1A 0x2F")]
    [InlineData("ERROR: ENOMEM - Cannot allocate memory")]
    [InlineData("#0 0x0800ABCD in main() at firmware.c:142")]
    [InlineData("Some random log message")]
    public async Task AnalyzeAsync_ReturnsNonEmptyExplanationAndSuggestions(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Explanation.Should().NotBeNullOrWhiteSpace();
        result.Suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var entry = MakeEntry("test");

        var act = () => _analyzer.AnalyzeAsync(entry, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
