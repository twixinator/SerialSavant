namespace SerialSavant.Config;

public sealed record AppSettings
{
    public required SerialConfig Serial { get; init; }
    public required LlmConfig Llm { get; init; }
}
