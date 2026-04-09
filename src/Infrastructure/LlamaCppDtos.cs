// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SerialSavant.Infrastructure;

public sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<ChatMessage> Messages { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; init; }
}

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("choices")]
    public required List<ChatChoice> Choices { get; init; }
}

public sealed class ChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("message")]
    public required ChatMessage Message { get; init; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

public sealed class LlmClassification
{
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; init; }

    [JsonPropertyName("suggestions")]
    public required List<string> Suggestions { get; init; }
}
