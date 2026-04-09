using System.Text.Json.Serialization;

namespace SerialSavant.Infrastructure;

[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
[JsonSerializable(typeof(LlmClassification))]
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public sealed partial class LlamaCppJsonContext : JsonSerializerContext;
