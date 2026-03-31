// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using SerialSavant.Core;

namespace SerialSavant.UI;

/// <summary>
/// Renders analyzed log entries to an output destination.
/// </summary>
/// <remarks>
/// Implementations must be thread-safe if called from a concurrent context.
/// Row-by-row rendering is the expected pattern — no live-display or cursor manipulation.
/// </remarks>
public interface ILogRenderer
{
    /// <summary>
    /// Renders the session header. Called once at startup before the first entry.
    /// </summary>
    void RenderHeader();

    /// <summary>
    /// Renders a single analyzed log entry as a permanent output row.
    /// </summary>
    /// <param name="entry">The raw log entry read from the source.</param>
    /// <param name="result">The analysis result produced by <see cref="ILlmAnalyzer"/>.</param>
    void Render(LogEntry entry, AnalysisResult result);

    /// <summary>
    /// Renders the session summary line. Called once after the stream ends cleanly.
    /// Not called on cancellation or error.
    /// </summary>
    /// <param name="totalCount">Number of entries successfully analyzed.</param>
    void RenderSummary(int totalCount);
}
