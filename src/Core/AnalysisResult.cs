// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// The result of analyzing a <see cref="LogEntry"/> via <see cref="ILlmAnalyzer"/>.
/// </summary>
/// <remarks>
/// Construct via <see cref="Create"/> to enforce invariants.
/// Note: C# <c>record with</c> expressions bypass <see cref="Create"/> validation.
/// Prefer immutable use over mutation via <c>with</c>.
/// </remarks>
public sealed record AnalysisResult
{
    /// <summary>Non-empty, non-whitespace description of the finding.</summary>
    public string Explanation { get; init; }

    /// <summary>Severity of the finding. Never <see cref="Severity.Unknown"/> in a completed result.</summary>
    public Severity Severity { get; init; }

    /// <summary>Non-empty list of actionable suggestions.</summary>
    public IReadOnlyList<string> Suggestions { get; init; }

    private AnalysisResult(string explanation, Severity severity, IReadOnlyList<string> suggestions)
    {
        Explanation = explanation;
        Severity = severity;
        Suggestions = suggestions;
    }

    /// <summary>
    /// Creates a validated <see cref="AnalysisResult"/>.
    /// </summary>
    /// <param name="explanation">Non-empty, non-whitespace description of the finding.</param>
    /// <param name="severity">Must not be <see cref="Severity.Unknown"/>.</param>
    /// <param name="suggestions">Non-null, non-empty list of actionable suggestions.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="explanation"/> is empty/whitespace, <paramref name="severity"/> is <see cref="Severity.Unknown"/>, or <paramref name="suggestions"/> is empty.</exception>
    public static AnalysisResult Create(
        string explanation,
        Severity severity,
        IReadOnlyList<string> suggestions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(explanation, nameof(explanation));
        if (severity is Severity.Unknown)
            throw new ArgumentException("Severity must not be Unknown for a completed analysis result.", nameof(severity));
        ArgumentNullException.ThrowIfNull(suggestions);
        if (suggestions.Count == 0)
            throw new ArgumentException("Suggestions must not be empty.", nameof(suggestions));

        return new AnalysisResult(explanation, severity, suggestions);
    }
}
