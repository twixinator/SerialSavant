// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Config;

namespace SerialSavant.Tests;

public sealed class LlmConfigValidatorTests
{
    private readonly LlmConfigValidator _validator = new();

    private static LlmConfig ValidConfig() => new()
    {
        ModelPath = "/models/llama.gguf",
        MaxTokens = 512,
        Temperature = 0.7f,
        ServerPort = 8080,
        TimeoutMs = 30_000
    };

    [Fact]
    public void Validate_ValidConfig_Succeeds()
    {
        var result = _validator.Validate(null, ValidConfig());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyModelPath_Fails(string modelPath)
    {
        var result = _validator.Validate(null, ValidConfig() with { ModelPath = modelPath });

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidMaxTokens_Fails(int maxTokens)
    {
        var result = _validator.Validate(null, ValidConfig() with { MaxTokens = maxTokens });

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Validate_TemperatureOutOfRange_Fails(float temperature)
    {
        var result = _validator.Validate(null, ValidConfig() with { Temperature = temperature });

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(2.0f)]
    public void Validate_TemperatureAtBoundary_Succeeds(float temperature)
    {
        var result = _validator.Validate(null, ValidConfig() with { Temperature = temperature });

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_InvalidServerPort_Fails(int port)
    {
        var result = _validator.Validate(null, ValidConfig() with { ServerPort = port });

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidTimeoutMs_Fails(int timeoutMs)
    {
        var result = _validator.Validate(null, ValidConfig() with { TimeoutMs = timeoutMs });

        result.Failed.Should().BeTrue();
    }
}
