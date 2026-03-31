// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

/// <summary>
/// Reads log entries from a serial port or a serial-port simulation.
/// </summary>
/// <remarks>
/// Implementations fall into two termination categories:
/// <list type="bullet">
///   <item><term>Finite</term><description>Completes after a fixed set of entries. Safe to enumerate without cancelling the token.</description></item>
///   <item><term>Infinite</term><description>Runs indefinitely. Callers MUST supply a <see cref="CancellationToken"/> they intend to cancel, or use <c>break</c> in the consuming <c>await foreach</c>.</description></item>
/// </list>
/// Whether repeated or concurrent enumeration is supported depends on the implementation.
/// </remarks>
public interface ISerialReader : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously enumerates log entries.
    /// </summary>
    /// <param name="cancellationToken">
    /// Used to stop the enumeration. <see cref="OperationCanceledException"/> is thrown when cancelled.
    /// Callers consuming an infinite implementation MUST cancel this token to terminate the stream.
    /// </param>
    IAsyncEnumerable<LogEntry> ReadAsync(CancellationToken cancellationToken = default);
}
