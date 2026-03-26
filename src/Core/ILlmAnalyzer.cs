namespace SerialSavant.Core;

public interface ILlmAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
