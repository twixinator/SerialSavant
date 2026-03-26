// SPDX-FileCopyrightText: 2026 raivolld
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string RawLine,
    SerialLogLevel LogLevel);
