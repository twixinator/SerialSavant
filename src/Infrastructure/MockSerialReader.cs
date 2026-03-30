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

    private static readonly (string RawLine, SerialLogLevel Level)[] DeterministicEntries =
    [
        ("0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD", SerialLogLevel.Debug),
        ("0xCA 0xFE 0xBA 0xBE 0x00 0x00 0x01", SerialLogLevel.Debug),
        ("0x7F 0x45 0x4C 0x46 0x02 0x01 0x01", SerialLogLevel.Info),
        ("ERROR: ENOMEM - Cannot allocate memory", SerialLogLevel.Error),
        ("ERROR: EACCES - Permission denied", SerialLogLevel.Error),
        ("FATAL: SIGSEGV - Segmentation fault at 0x00000010", SerialLogLevel.Fatal),
        ("FATAL: SIGBUS - Bus error at 0x0000DEAD", SerialLogLevel.Fatal),
        ("#0 0x0800ABCD in main() at firmware.c:142", SerialLogLevel.Error),
        ("#1 0x0800EF01 in init_hardware() at hal.c:87", SerialLogLevel.Error),
        ("#2 0x08001234 in reset_handler() at startup.s:12", SerialLogLevel.Warning),
    ];

    public async IAsyncEnumerable<LogEntry> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_mode is MockMode.Deterministic)
        {
            foreach (var (rawLine, level) in DeterministicEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new LogEntry(_timeProvider.GetUtcNow(), rawLine, level);

                if (_delay is { } d)
                {
                    await Task.Delay(d, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else
        {
            // Random mode — implemented in Task 4
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
