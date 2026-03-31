// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Options;

namespace SerialSavant.Config;

public sealed class LlmConfigValidator : IValidateOptions<LlmConfig>
{
    private const float TEMPERATURE_MIN = 0.0f;
    private const float TEMPERATURE_MAX = 2.0f;
    private const int SERVER_PORT_MIN = 1;
    private const int SERVER_PORT_MAX = 65535;

    public ValidateOptionsResult Validate(string? name, LlmConfig options)
    {
        if (string.IsNullOrWhiteSpace(options.ModelPath))
            return ValidateOptionsResult.Fail($"{nameof(LlmConfig.ModelPath)} must not be empty.");

        if (options.MaxTokens <= 0)
            return ValidateOptionsResult.Fail($"{nameof(LlmConfig.MaxTokens)} must be greater than zero.");

        if (options.Temperature < TEMPERATURE_MIN || options.Temperature > TEMPERATURE_MAX)
            return ValidateOptionsResult.Fail(
                $"{nameof(LlmConfig.Temperature)} must be in [{TEMPERATURE_MIN}, {TEMPERATURE_MAX}], got {options.Temperature}.");

        if (options.ServerPort < SERVER_PORT_MIN || options.ServerPort > SERVER_PORT_MAX)
            return ValidateOptionsResult.Fail(
                $"{nameof(LlmConfig.ServerPort)} must be in [{SERVER_PORT_MIN}, {SERVER_PORT_MAX}], got {options.ServerPort}.");

        if (options.TimeoutMs <= 0)
            return ValidateOptionsResult.Fail($"{nameof(LlmConfig.TimeoutMs)} must be greater than zero.");

        return ValidateOptionsResult.Success;
    }
}
