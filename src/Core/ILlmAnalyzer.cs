// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// Analyzes a serial log entry and returns a severity classification with explanation and suggestions.
/// </summary>
public interface ILlmAnalyzer
{
    /// <summary>
    /// Analyzes a single log entry.
    /// </summary>
    /// <param name="entry">The log entry to analyze. Must not be <c>null</c>.</param>
    /// <param name="cancellationToken">
    /// Used to cancel a long-running or network-bound analysis.
    /// <see cref="OperationCanceledException"/> is thrown if cancelled before or during the operation.
    /// </param>
    /// <returns>
    /// A completed <see cref="AnalysisResult"/> with a non-<see cref="Severity.Unknown"/> severity,
    /// a non-empty explanation, and at least one suggestion.
    /// </returns>
    Task<AnalysisResult> AnalyzeAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
