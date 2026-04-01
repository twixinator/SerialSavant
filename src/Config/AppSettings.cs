// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record AppSettings
{
    public required SerialConfig Serial { get; init; }
    public required LlmConfig Llm { get; init; }

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
