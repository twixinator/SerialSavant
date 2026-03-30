// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed partial class MockLlmAnalyzer : ILlmAnalyzer
{
    private static readonly string[] CriticalSignals = ["SIGSEGV", "SIGBUS"];

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

            return new AnalysisResult(
                Explanation: $"POSIX error detected: {errnoName}. This indicates a system-level failure that may require immediate attention.",
                Severity: isCritical ? Severity.Critical : Severity.High,
                Suggestions: isCritical
                    ? ["Inspect pointer validity", "Check for stack overflow", "Review memory map"]
                    : ["Check memory allocation", "Verify resource permissions", "Review system limits"]);
        }

        if (StackTracePattern().IsMatch(rawLine))
        {
            return new AnalysisResult(
                Explanation: "Stack trace frame detected. This indicates a crash or exception in the firmware execution path.",
                Severity: Severity.High,
                Suggestions: ["Check stack depth", "Review function at crash point", "Inspect call chain for recursion"]);
        }

        if (HexDumpPattern().IsMatch(rawLine))
        {
            return new AnalysisResult(
                Explanation: "Hex dump detected. Raw byte data from device memory or communication buffer.",
                Severity: Severity.Low,
                Suggestions: ["Check byte alignment", "Verify endianness"]);
        }

        return new AnalysisResult(
            Explanation: "Unrecognized log entry. Unable to determine specific pattern.",
            Severity: Severity.Medium,
            Suggestions: ["Review log context"]);
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
