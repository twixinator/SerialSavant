// SPDX-FileCopyrightText: 2026 raivolld
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public sealed record AppSettings
{
    public required SerialConfig Serial { get; init; }
    public required LlmConfig Llm { get; init; }
}
