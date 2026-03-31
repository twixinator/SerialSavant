// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Config;

namespace SerialSavant.Tests;

public sealed class SerialConfigValidatorTests
{
    private readonly SerialConfigValidator _validator = new();

    private static SerialConfig ValidConfig() => new()
    {
        Port = "COM3",
        BaudRate = 115200,
        ReconnectBaseDelayMs = 500,
        ReconnectMaxDelayMs = 30_000,
        ReconnectMaxAttempts = 10
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
    public void Validate_EmptyPort_Fails(string port)
    {
        var result = _validator.Validate(null, ValidConfig() with { Port = port });

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidBaudRate_Fails(int baudRate)
    {
        var result = _validator.Validate(null, ValidConfig() with { BaudRate = baudRate });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_BaseDelayGreaterThanMaxDelay_Fails()
    {
        var config = ValidConfig() with { ReconnectBaseDelayMs = 60_000, ReconnectMaxDelayMs = 500 };

        var result = _validator.Validate(null, config);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeMaxAttempts_Fails()
    {
        var result = _validator.Validate(null, ValidConfig() with { ReconnectMaxAttempts = -1 });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroMaxAttempts_Succeeds()
    {
        // Zero means "no reconnect attempts" — a valid operational choice.
        var result = _validator.Validate(null, ValidConfig() with { ReconnectMaxAttempts = 0 });

        result.Succeeded.Should().BeTrue();
    }
}
