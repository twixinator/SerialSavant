// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record AppSettings
{
    public required SerialConfig Serial { get; init; }
    public required LlmConfig Llm { get; init; }

    /// <summary>
    /// Creates a new <see cref="AppSettings"/> with platform-aware defaults.
    /// </summary>
    /// <remarks>
    /// <para><see cref="SerialConfig.Port"/> is <c>"COM1"</c> on Windows and
    /// <c>"/dev/ttyUSB0"</c> on Linux/macOS. This factory is the only place the
    /// platform split is applied; the <see cref="SerialConfig"/> property-level
    /// default is always <c>"/dev/ttyUSB0"</c> and is intended for deserialization
    /// fallback only.</para>
    /// <para><see cref="LlmConfig.ModelPath"/> is intentionally empty. Callers that
    /// receive <c>WasDefaulted = true</c> from
    /// <see cref="AppSettingsRepository.LoadAsync"/> should prompt the user to
    /// supply a model path before invoking the LLM subsystem.</para>
    /// </remarks>
    public static AppSettings CreateDefault() => new()
    {
        Serial = new SerialConfig
        {
            Port = OperatingSystem.IsWindows() ? "COM1" : "/dev/ttyUSB0",
            BaudRate = 115200,
        },
        Llm = new LlmConfig
        {
            ModelPath = string.Empty,
        },
    };
}
