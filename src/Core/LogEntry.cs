// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string RawLine,
    SerialLogLevel LogLevel);
