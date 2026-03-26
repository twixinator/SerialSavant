// SPDX-FileCopyrightText: 2026 raivolld
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record SerialConfig
{
    public required string Port { get; init; }
    public required int BaudRate { get; init; }
    public int ReconnectBaseDelayMs { get; init; } = 500;
    public int ReconnectMaxDelayMs { get; init; } = 30_000;
    public int ReconnectMaxAttempts { get; init; } = 10;
}
