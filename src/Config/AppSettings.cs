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
    /// <c>"/dev/ttyUSB0"</c> on Linux/macOS.</para>
    /// <para><see cref="LlmConfig.ModelPath"/> is intentionally empty — the UI
    /// checks <c>WasDefaulted</c> and prompts the user to configure the model
    /// path before the LLM subsystem is used.</para>
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
