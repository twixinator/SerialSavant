// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Options;

namespace SerialSavant.Config;

public sealed class SerialConfigValidator : IValidateOptions<SerialConfig>
{
    public ValidateOptionsResult Validate(string? name, SerialConfig options)
    {
        if (string.IsNullOrWhiteSpace(options.Port))
            return ValidateOptionsResult.Fail($"{nameof(SerialConfig.Port)} must not be empty.");

        if (options.BaudRate <= 0)
            return ValidateOptionsResult.Fail($"{nameof(SerialConfig.BaudRate)} must be greater than zero.");

        if (options.ReconnectBaseDelayMs > options.ReconnectMaxDelayMs)
            return ValidateOptionsResult.Fail(
                $"{nameof(SerialConfig.ReconnectBaseDelayMs)} ({options.ReconnectBaseDelayMs}) " +
                $"must not exceed {nameof(SerialConfig.ReconnectMaxDelayMs)} ({options.ReconnectMaxDelayMs}).");

        if (options.ReconnectMaxAttempts < 0)
            return ValidateOptionsResult.Fail(
                $"{nameof(SerialConfig.ReconnectMaxAttempts)} must be zero or greater. Zero means no reconnect.");

        return ValidateOptionsResult.Success;
    }
}
