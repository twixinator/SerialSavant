// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Infrastructure;

/// <summary>
/// Selects the behavior mode for <see cref="MockSerialReader"/>.
/// </summary>
public enum MockMode
{
    /// <summary>
    /// Emits a fixed sequence of 10 entries then completes.
    /// Safe to enumerate without cancelling the token.
    /// </summary>
    Deterministic,

    /// <summary>
    /// Emits an infinite stream of randomly generated entries.
    /// Callers MUST cancel the <see cref="System.Threading.CancellationToken"/>
    /// passed to <see cref="MockSerialReader.ReadAsync"/> to terminate the stream.
    /// </summary>
    Random
}
