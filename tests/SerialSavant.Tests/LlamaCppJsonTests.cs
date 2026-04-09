using System.Text.Json;
using AwesomeAssertions;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class LlamaCppJsonTests
{
    [Fact]
    public void Given_ChatRequest_When_Serialized_Then_RoundTrips()
    {
        var request = new ChatCompletionRequest
        {
            Model = "local",
            Messages =
            [
                new ChatMessage { Role = "system", Content = "You are a log analyzer." },
                new ChatMessage { Role = "user", Content = "ERROR: ENOMEM" }
            ],
            MaxTokens = 512,
            Temperature = 0.7f
        };

        var json = JsonSerializer.Serialize(request, LlamaCppJsonContext.Default.ChatCompletionRequest);
        var deserialized = JsonSerializer.Deserialize(json, LlamaCppJsonContext.Default.ChatCompletionRequest);

        deserialized.Should().NotBeNull();
        deserialized!.Model.Should().Be("local");
        deserialized.Messages.Should().HaveCount(2);
        deserialized.MaxTokens.Should().Be(512);
    }

    [Fact]
    public void Given_ChatResponse_When_Deserialized_Then_ExtractsContent()
    {
        const string json = """
        {
            "id": "chatcmpl-123",
            "object": "chat.completion",
            "choices": [
                {
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "{\"severity\":\"High\",\"explanation\":\"Memory allocation failure\",\"suggestions\":[\"Check heap size\"]}"
                    },
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize(json, LlamaCppJsonContext.Default.ChatCompletionResponse);

        response.Should().NotBeNull();
        response!.Choices.Should().HaveCount(1);
        response.Choices[0].Message.Content.Should().Contain("Memory allocation failure");
    }

    [Fact]
    public void Given_LlmClassification_When_Deserialized_Then_MapsFields()
    {
        const string json = """
        {
            "severity": "High",
            "explanation": "POSIX error detected",
            "suggestions": ["Check memory", "Review limits"]
        }
        """;

        var result = JsonSerializer.Deserialize(json, LlamaCppJsonContext.Default.LlmClassification);

        result.Should().NotBeNull();
        result!.Severity.Should().Be("High");
        result.Explanation.Should().Be("POSIX error detected");
        result.Suggestions.Should().HaveCount(2);
    }
}
