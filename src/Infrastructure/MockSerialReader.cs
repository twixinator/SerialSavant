// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class MockSerialReader(
    MockMode mode,
    TimeSpan? delay = null,
    TimeProvider? timeProvider = null,
    Random? random = null) : ISerialReader
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Random _random = random ?? new Random();
    private readonly TimeSpan? _delay = delay;
    private readonly MockMode _mode = mode;

    public async IAsyncEnumerable<LogEntry> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Minimal: will be implemented in TDD steps
        await Task.CompletedTask;
        yield break;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
