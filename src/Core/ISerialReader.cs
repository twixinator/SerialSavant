// SPDX-FileCopyrightText: 2026 raivolld
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Core;

public interface ISerialReader : IAsyncDisposable
{
    IAsyncEnumerable<LogEntry> ReadAsync(CancellationToken cancellationToken = default);
}
