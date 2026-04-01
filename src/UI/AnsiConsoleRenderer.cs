// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using SerialSavant.Core;
using Spectre.Console;

namespace SerialSavant.UI;

/// <summary>
/// Renders log entries to the ANSI console using Spectre.Console markup.
/// Each entry is printed as a permanent row — no live-display or cursor manipulation.
/// Terminal emulator scrollback handles history natively.
/// </summary>
public sealed class AnsiConsoleRenderer : ILogRenderer
{
    private const int SEPARATOR_WIDTH = 72;

    private static string SeverityColor(Severity severity) => severity switch
    {
        Severity.Low => "green",
        Severity.Medium => "yellow",
        Severity.High => "red",
        Severity.Critical => "bold red",
        _ => "white",
    };

    /// <inheritdoc/>
    public void RenderHeader()
    {
        AnsiConsole.MarkupLine("[bold cyan]SerialSavant — UART Log Analyzer[/]");
        AnsiConsole.MarkupLine("[grey]" + new string('─', SEPARATOR_WIDTH) + "[/]");
        AnsiConsole.MarkupLine("[grey dim] Timestamp        Level      Sev       Raw line / Analysis[/]");
        AnsiConsole.MarkupLine("[grey]" + new string('─', SEPARATOR_WIDTH) + "[/]");
    }

    /// <inheritdoc/>
    public void Render(LogEntry entry, AnalysisResult result)
    {
        var color = SeverityColor(result.Severity);
        var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        var level = $"{entry.LogLevel,-9}";
        var severity = $"{result.Severity,-8}";
        var rawLine = Markup.Escape(entry.RawLine);
        var explanation = Markup.Escape(result.Explanation);

        AnsiConsole.MarkupLine(
            $"[grey] {timestamp}[/] [[{level}]] [{color}]{severity}[/]  [{color}]{rawLine}[/]");
        AnsiConsole.MarkupLine(
            $"[dim]  └─ {explanation}[/]");
    }

    /// <inheritdoc/>
    public void RenderSummary(int totalCount)
    {
        AnsiConsole.MarkupLine("[grey]" + new string('─', SEPARATOR_WIDTH) + "[/]");
        AnsiConsole.MarkupLine($"[bold]Session complete — {totalCount} entries analyzed.[/]");
    }
}
