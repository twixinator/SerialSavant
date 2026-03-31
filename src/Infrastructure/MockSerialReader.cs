// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class MockSerialReader(
    MockMode mode,
    TimeSpan? delay = null,
    TimeProvider? timeProvider = null,
    Random? random = null,
    ILogger<MockSerialReader>? logger = null) : ISerialReader
{
    private const int CATEGORY_COUNT = 3;
    private const int MAX_BYTE_VALUE = 256;
    private const int MAX_ERRNO_ARG = 65536;
    private const int FIRMWARE_ADDRESS_MIN = 0x08000000;
    private const int FIRMWARE_ADDRESS_MAX = 0x08FFFFFF;
    private const int MAX_SOURCE_LINE = 500;

    // ValidateMode runs at field-initializer time (i.e. construction), giving
    // an ArgumentOutOfRangeException before any other fields are set.
    private readonly MockMode _mode = ValidateMode(mode);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Random _random = random ?? new Random();
    private readonly TimeSpan? _delay = delay;
    private readonly ILogger<MockSerialReader> _logger = logger ?? NullLogger<MockSerialReader>.Instance;

    // (template, argCount) pairs — argCount is explicit to avoid fragile '{' character counting.
    private static readonly (string Template, int ArgCount)[] HexTemplates =
    [
        ("0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2} 0x{4:X2} 0x{5:X2}", 6),
        ("0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2}", 4),
    ];

    private static readonly (string Template, SerialLogLevel Level)[] ErrnoTemplates =
    [
        ("ERROR: ENOMEM - Cannot allocate memory (requested {0} bytes)", SerialLogLevel.Error),
        ("ERROR: EACCES - Permission denied for resource {0}", SerialLogLevel.Error),
        ("ERROR: ETIMEDOUT - Connection timed out after {0}ms", SerialLogLevel.Error),
        ("FATAL: SIGSEGV - Segmentation fault at 0x{0:X8}", SerialLogLevel.Fatal),
        ("FATAL: SIGBUS - Bus error at 0x{0:X8}", SerialLogLevel.Fatal),
    ];

    private static readonly (string Template, int ArgCount)[] StackTemplates =
    [
        ("#0 0x{0:X8} in {1}() at {2}:{3}", 4),
        ("#1 0x{0:X8} in {1}() at {2}:{3}", 4),
    ];

    private static readonly string[] FunctionNames =
        ["main", "init_hardware", "read_sensor", "write_flash", "reset_handler"];

    private static readonly string[] FileNames =
        ["firmware.c", "hal.c", "sensor.c", "flash.c", "startup.s"];

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
            await foreach (var entry in ReadDeterministicAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
        else
        {
            _logger.LogDebug(
                "MockSerialReader entering Random mode (delayMs={DelayMs})",
                _delay.HasValue ? (object)_delay.Value.TotalMilliseconds : "none");

            await foreach (var entry in ReadRandomAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }

    private async IAsyncEnumerable<LogEntry> ReadDeterministicAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
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

    private async IAsyncEnumerable<LogEntry> ReadRandomAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_delay is { } period)
        {
            // PeriodicTimer waits one full period before each tick, including the first.
            using var timer = new PeriodicTimer(period);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return GenerateRandomEntry();
            }
        }
        else
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return GenerateRandomEntry();
                // Yield to the scheduler for cooperative multitasking in tight loops.
                await Task.Yield();
            }
        }
    }

    private LogEntry GenerateRandomEntry()
    {
        var category = _random.Next(CATEGORY_COUNT);
        string rawLine;
        SerialLogLevel level;

        switch (category)
        {
            case 0: // Hex dump
                {
                    var (template, argCount) = HexTemplates[_random.Next(HexTemplates.Length)];
                    var args = new object[argCount];
                    for (var i = 0; i < argCount; i++)
                        args[i] = _random.Next(MAX_BYTE_VALUE);
                    rawLine = string.Format(template, args);
                    level = SerialLogLevel.Debug;
                    break;
                }
            case 1: // Errno / signal
                {
                    var (template, errLevel) = ErrnoTemplates[_random.Next(ErrnoTemplates.Length)];
                    rawLine = string.Format(template, _random.Next(1, MAX_ERRNO_ARG));
                    level = errLevel;
                    break;
                }
            case 2: // Stack trace
                {
                    var (template, argCount) = StackTemplates[_random.Next(StackTemplates.Length)];
                    var args = new object[argCount];
                    args[0] = _random.Next(FIRMWARE_ADDRESS_MIN, FIRMWARE_ADDRESS_MAX);
                    args[1] = FunctionNames[_random.Next(FunctionNames.Length)];
                    args[2] = FileNames[_random.Next(FileNames.Length)];
                    args[3] = _random.Next(1, MAX_SOURCE_LINE);
                    rawLine = string.Format(template, args);
                    level = SerialLogLevel.Error;
                    break;
                }
            default:
                throw new UnreachableException($"Unexpected category value: {category}");
        }

        return new LogEntry(_timeProvider.GetUtcNow(), rawLine, level);
    }

    /// <summary>
    /// No-op. This mock holds no unmanaged resources.
    /// To stop an active <see cref="ReadAsync"/> stream, cancel the
    /// <see cref="CancellationToken"/> passed to <see cref="ReadAsync"/>.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static MockMode ValidateMode(MockMode mode)
    {
        if (mode is not (MockMode.Deterministic or MockMode.Random))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid MockMode value.");
        return mode;
    }
}
