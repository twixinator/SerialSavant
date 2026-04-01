// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Infrastructure;

/// <summary>
/// Carries the resolved <see cref="MockMode"/> into the DI container.
/// Populated from command-line arguments before host construction.
/// </summary>
public sealed class MockOptions
{
    /// <summary>
    /// The <see cref="MockMode"/> to use: <see cref="MockMode.Random"/> when
    /// <c>--mock</c> is set; <see cref="MockMode.Deterministic"/> otherwise.
    /// </summary>
    public required MockMode Mode { get; init; }
}
