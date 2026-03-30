// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

public interface ISerialReader : IAsyncDisposable
{
    IAsyncEnumerable<LogEntry> ReadAsync(CancellationToken cancellationToken = default);
}
