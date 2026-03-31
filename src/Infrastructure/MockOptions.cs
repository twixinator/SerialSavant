// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Infrastructure;

/// <summary>
/// Carries the parsed <c>--mock</c> flag into the DI container.
/// Populated from command-line arguments before host construction.
/// </summary>
public sealed class MockOptions
{
    /// <summary>
    /// When <see langword="true"/>, <see cref="MockSerialReader"/> runs in
    /// <see cref="MockMode.Random"/> (infinite stream).
    /// When <see langword="false"/>, runs in <see cref="MockMode.Deterministic"/>
    /// (10 entries, then exits).
    /// </summary>
    public bool IsMockFlagSet { get; init; }
}
