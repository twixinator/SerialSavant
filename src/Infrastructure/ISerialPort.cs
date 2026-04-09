// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Infrastructure;

/// <summary>
/// Thin abstraction over a serial port connection for testability.
/// </summary>
public interface ISerialPort : IDisposable
{
    void Open();
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
    bool IsOpen { get; }
}
