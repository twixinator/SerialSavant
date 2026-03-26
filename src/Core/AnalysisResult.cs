namespace SerialSavant.Core;

public sealed record AnalysisResult(
    string Explanation,
    Severity Severity,
    IReadOnlyList<string> Suggestions);
