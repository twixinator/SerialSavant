using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SerialSavant.Config;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class LlamaCppAnalyzer(
    IHttpClientFactory httpClientFactory,
    ILlamaServerGate serverGate,
    IOptions<LlmConfig> options,
    ILogger<LlamaCppAnalyzer> logger) : ILlmAnalyzer
{
    private const string SystemPrompt =
        """You are an embedded systems log analyzer. For each log entry, respond with a JSON object: { "severity": "Low|Medium|High|Critical", "explanation": "<why this matters>", "suggestions": ["<action1>", "<action2>"] }. Be concise.""";

    private readonly LlmConfig _config = options.Value;

    public async Task<AnalysisResult> AnalyzeAsync(
        LogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        await serverGate.WaitForReadyAsync(cancellationToken).ConfigureAwait(false);

        var request = new ChatCompletionRequest
        {
            Model = "local",
            Messages =
            [
                new ChatMessage { Role = "system", Content = SystemPrompt },
                new ChatMessage { Role = "user", Content = entry.RawLine }
            ],
            MaxTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        var requestJson = JsonSerializer.Serialize(request, LlamaCppJsonContext.Default.ChatCompletionRequest);
        using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        using var httpClient = httpClientFactory.CreateClient("llama");
        using var response = await httpClient.PostAsync(
            "/v1/chat/completions", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var chatResponse = await JsonSerializer.DeserializeAsync(
            responseStream, LlamaCppJsonContext.Default.ChatCompletionResponse,
            cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from llama-server.");

        var assistantContent = chatResponse.Choices[0].Message.Content;
        return ParseClassification(assistantContent);
    }

    private static AnalysisResult ParseClassification(string content)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd <= jsonStart)
                return CreateFallback(content);

            var jsonSpan = content[jsonStart..(jsonEnd + 1)];
            var classification = JsonSerializer.Deserialize(
                jsonSpan, LlamaCppJsonContext.Default.LlmClassification);

            if (classification is null)
                return CreateFallback(content);

            var severity = Enum.TryParse<Severity>(classification.Severity, ignoreCase: true, out var parsed)
                ? parsed
                : Severity.Medium;

            if (severity is Severity.Unknown)
                severity = Severity.Medium;

            var explanation = string.IsNullOrWhiteSpace(classification.Explanation)
                ? content
                : classification.Explanation;

            var suggestions = classification.Suggestions?.Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
                ?? [];

            if (suggestions.Count == 0)
                suggestions = ["Review log entry manually"];

            return AnalysisResult.Create(explanation, severity, suggestions);
        }
        catch (JsonException)
        {
            return CreateFallback(content);
        }
    }

    private static AnalysisResult CreateFallback(string rawContent) =>
        AnalysisResult.Create(
            explanation: string.IsNullOrWhiteSpace(rawContent) ? "No analysis available" : rawContent,
            severity: Severity.Medium,
            suggestions: ["Review log entry manually"]);
}
