// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// A single line read from the serial port, with its parsed metadata.
/// </summary>
/// <param name="Timestamp">UTC time the line was received. Uses <see cref="DateTimeOffset"/> to preserve timezone context.</param>
/// <param name="RawLine">The raw string read from the serial port. May be empty for blank device output lines.</param>
/// <param name="LogLevel">
/// The device-side log level parsed from <paramref name="RawLine"/>.
/// <see cref="SerialLogLevel.Unknown"/> if the level cannot be determined.
/// </param>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string RawLine,
    SerialLogLevel LogLevel);
