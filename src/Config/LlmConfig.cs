// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record LlmConfig
{
    public string ModelPath { get; init; } = string.Empty;
    public int MaxTokens { get; init; } = 512;
    public float Temperature { get; init; } = 0.7f;
    public int ServerPort { get; init; } = 8080;
    public int TimeoutMs { get; init; } = 30_000;
    public string ServerPath { get; init; } = "llama-server";
}
