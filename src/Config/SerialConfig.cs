// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record SerialConfig
{
    public string Port { get; init; } = "/dev/ttyUSB0";
    public int BaudRate { get; init; } = 115200;
    public int ReconnectBaseDelayMs { get; init; } = 500;
    public int ReconnectMaxDelayMs { get; init; } = 30_000;
    public int ReconnectMaxAttempts { get; init; } = 10;
}
