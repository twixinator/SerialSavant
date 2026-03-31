// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed partial class MockLlmAnalyzer : ILlmAnalyzer
{
    // Signals that map to Severity.Critical. All other entries in ErrnoNames
    // (POSIX errno codes and non-critical signals) map to Severity.High.
    private static readonly string[] CriticalSignals = ["SIGSEGV", "SIGBUS", "SIGABRT"];

    // POSIX errno codes (ENOMEM, EACCES, ETIMEDOUT, EINVAL, ENOENT) and
    // fatal signals (SIGSEGV, SIGBUS, SIGABRT). Signals in CriticalSignals
    // above map to Severity.Critical; all others map to Severity.High.
    private static readonly string[] ErrnoNames =
        ["ENOMEM", "EACCES", "ETIMEDOUT", "EINVAL", "ENOENT", "SIGSEGV", "SIGBUS", "SIGABRT"];

    public Task<AnalysisResult> AnalyzeAsync(
        LogEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rawLine = entry.RawLine;
        var result = Classify(rawLine);

        return Task.FromResult(result);
    }

    private static AnalysisResult Classify(string rawLine)
    {
        if (ContainsErrno(rawLine, out var errnoName))
        {
            var isCritical = CriticalSignals.Any(s =>
                errnoName.Contains(s, StringComparison.Ordinal));

            return AnalysisResult.Create(
                explanation: $"POSIX error detected: {errnoName}. This indicates a system-level failure that may require immediate attention.",
                severity: isCritical ? Severity.Critical : Severity.High,
                suggestions: isCritical
                    ? ["Inspect pointer validity", "Check for stack overflow", "Review memory map"]
                    : ["Check memory allocation", "Verify resource permissions", "Review system limits"]);
        }

        if (StackTracePattern().IsMatch(rawLine))
        {
            return AnalysisResult.Create(
                explanation: "Stack trace frame detected. This indicates a crash or exception in the firmware execution path.",
                severity: Severity.High,
                suggestions: ["Check stack depth", "Review function at crash point", "Inspect call chain for recursion"]);
        }

        if (HexDumpPattern().IsMatch(rawLine))
        {
            return AnalysisResult.Create(
                explanation: "Hex dump detected. Raw byte data from device memory or communication buffer.",
                severity: Severity.Low,
                suggestions: ["Check byte alignment", "Verify endianness"]);
        }

        return AnalysisResult.Create(
            explanation: "Unrecognized log entry. Unable to determine specific pattern.",
            severity: Severity.Medium,
            suggestions: ["Review log context"]);
    }

    private static bool ContainsErrno(string rawLine, out string errnoName)
    {
        foreach (var name in ErrnoNames)
        {
            if (rawLine.Contains(name, StringComparison.Ordinal))
            {
                errnoName = name;
                return true;
            }
        }

        errnoName = string.Empty;
        return false;
    }

    [GeneratedRegex(@"#\d+\s+0x[0-9A-Fa-f]+\s+in\s+")]
    private static partial Regex StackTracePattern();

    [GeneratedRegex(@"0x[0-9A-Fa-f]{2}(\s+0x[0-9A-Fa-f]{2})+")]
    private static partial Regex HexDumpPattern();
}
