// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// The log level reported by the embedded device on the serial line.
/// This is the device's own classification, distinct from <see cref="Severity"/>, which is the analyzer's assessment.
/// Values are ordered by ascending severity: <see cref="Debug"/> → <see cref="Info"/> → <see cref="Warning"/> → <see cref="Error"/> → <see cref="Fatal"/>.
/// </summary>
public enum SerialLogLevel
{
    /// <summary>Default/sentinel value. Assigned when the device-side level cannot be parsed from the raw line.</summary>
    Unknown = 0,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
