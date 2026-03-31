// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// Severity level assigned to a log entry by <see cref="ILlmAnalyzer"/>.
/// Values are ordered by ascending severity: <see cref="Low"/> → <see cref="Medium"/> → <see cref="High"/> → <see cref="Critical"/>.
/// </summary>
public enum Severity
{
    /// <summary>Default/sentinel value. A completed <see cref="AnalysisResult"/> must never carry this value.</summary>
    Unknown = 0,
    /// <summary>Informational — no immediate action required.</summary>
    Low,
    /// <summary>Noteworthy — monitor or investigate.</summary>
    Medium,
    /// <summary>Significant failure — action recommended.</summary>
    High,
    /// <summary>Fatal or unrecoverable failure — immediate action required.</summary>
    Critical
}
