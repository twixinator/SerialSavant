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
            var hexTemplates = new[]
            {
                "0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2} 0x{4:X2} 0x{5:X2}",
                "0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2}",
            };

            var errnoTemplates = new[]
            {
                ("ERROR: ENOMEM - Cannot allocate memory (requested {0} bytes)", SerialLogLevel.Error),
                ("ERROR: EACCES - Permission denied for resource {0}", SerialLogLevel.Error),
                ("ERROR: ETIMEDOUT - Connection timed out after {0}ms", SerialLogLevel.Error),
                ("FATAL: SIGSEGV - Segmentation fault at 0x{0:X8}", SerialLogLevel.Fatal),
                ("FATAL: SIGBUS - Bus error at 0x{0:X8}", SerialLogLevel.Fatal),
            };

            var stackTemplates = new[]
            {
                "#0 0x{0:X8} in {1}() at {2}:{3}",
                "#1 0x{0:X8} in {1}() at {2}:{3}",
            };

            var functionNames = new[] { "main", "init_hardware", "read_sensor", "write_flash", "reset_handler" };
            var fileNames = new[] { "firmware.c", "hal.c", "sensor.c", "flash.c", "startup.s" };

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var category = _random.Next(3);
                string rawLine;
                SerialLogLevel level;

                switch (category)
                {
                    case 0: // Hex dump
                        {
                            var template = hexTemplates[_random.Next(hexTemplates.Length)];
                            var byteCount = template.Count(c => c == '{');
                            var args = Enumerable.Range(0, byteCount)
                                .Select(_ => (object)_random.Next(256)).ToArray();
                            rawLine = string.Format(template, args);
                            level = SerialLogLevel.Debug;
                            break;
                        }
                    case 1: // Errno
                        {
                            var (template, errLevel) = errnoTemplates[_random.Next(errnoTemplates.Length)];
                            rawLine = string.Format(template, _random.Next(1, 65536));
                            level = errLevel;
                            break;
                        }
                    default: // Stack trace
                        {
                            var template = stackTemplates[_random.Next(stackTemplates.Length)];
                            rawLine = string.Format(
                                template,
                                _random.Next(0x08000000, 0x08FFFFFF),
                                functionNames[_random.Next(functionNames.Length)],
                                fileNames[_random.Next(fileNames.Length)],
                                _random.Next(1, 500));
                            level = SerialLogLevel.Error;
                            break;
                        }
                }

                yield return new LogEntry(_timeProvider.GetUtcNow(), rawLine, level);

                if (_delay is { } d)
                {
                    await Task.Delay(d, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
