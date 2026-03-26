namespace SerialSavant.Config;

public sealed record LlmConfig
{
    public required string ModelPath { get; init; }
    public int MaxTokens { get; init; } = 512;
    public float Temperature { get; init; } = 0.7f;
    public int ServerPort { get; init; } = 8080;
    public int TimeoutMs { get; init; } = 30_000;
}
