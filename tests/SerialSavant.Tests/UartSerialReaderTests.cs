using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SerialSavant.Config;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

#region Test Fakes

internal sealed class FakeSerialPort(Queue<string?> lines, Exception? throwAfter = null) : ISerialPort
{
    public bool IsOpen { get; private set; }
    public bool Disposed { get; private set; }

    public void Open() => IsOpen = true;

    public Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (lines.Count == 0 && throwAfter is not null)
            throw throwAfter;

        if (lines.Count == 0)
            return Task.FromResult<string?>(null);

        return Task.FromResult(lines.Dequeue());
    }

    public void Dispose()
    {
        IsOpen = false;
        Disposed = true;
    }
}

internal sealed class FakeSerialPortFactory(Queue<ISerialPort> ports) : ISerialPortFactory
{
    public int CreateCallCount { get; private set; }

    public ISerialPort Create(string portName, int baudRate)
    {
        CreateCallCount++;
        return ports.Dequeue();
    }
}

internal sealed class FailThenSucceedFactory(int failCount, FakeSerialPort successPort) : ISerialPortFactory
{
    private int _attempt;

    public ISerialPort Create(string portName, int baudRate)
    {
        _attempt++;
        if (_attempt <= failCount)
            return new ThrowingSerialPort();
        return successPort;
    }
}

internal sealed class ThrowingSerialPort : ISerialPort
{
    public bool IsOpen => false;
    public void Open() => throw new IOException("Port not available");
    public Task<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Not open");
    public void Dispose() { }
}

internal sealed class BlockingSerialPort : ISerialPort
{
    public bool IsOpen { get; private set; }
    public void Open() => IsOpen = true;

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        return null; // unreachable
    }

    public void Dispose() => IsOpen = false;
}

#endregion

public sealed class UartSerialReaderTests
{
    private static readonly SerialConfig DefaultConfig = new()
    {
        Port = "COM3",
        BaudRate = 115200,
        ReconnectBaseDelayMs = 10,   // fast for tests
        ReconnectMaxDelayMs = 100,
        ReconnectMaxAttempts = 3
    };

    [Fact]
    public async Task Given_WorkingPort_When_ReadAsync_Then_YieldsLogEntries()
    {
        var lines = new Queue<string?>(["ERROR: ENOMEM", "INFO: boot ok", null]);
        var port = new FakeSerialPort(lines);
        var factory = new FakeSerialPortFactory(new Queue<ISerialPort>([port]));

        await using var reader = new UartSerialReader(factory, DefaultConfig,
            NullLogger<UartSerialReader>.Instance);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().HaveCount(2);
        entries[0].LogLevel.Should().Be(SerialLogLevel.Error);
        entries[0].RawLine.Should().Be("ERROR: ENOMEM");
        entries[1].LogLevel.Should().Be(SerialLogLevel.Info);
    }

    [Fact]
    public async Task Given_PortFailsThenRecovers_When_ReadAsync_Then_ReconnectsAndContinues()
    {
        var successLines = new Queue<string?>(["DEBUG: ok", null]);
        var successPort = new FakeSerialPort(successLines);
        var factory = new FailThenSucceedFactory(failCount: 2, successPort);

        await using var reader = new UartSerialReader(factory, DefaultConfig,
            NullLogger<UartSerialReader>.Instance);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().HaveCount(1);
        entries[0].RawLine.Should().Be("DEBUG: ok");
    }

    [Fact]
    public async Task Given_AllReconnectsFail_When_ReadAsync_Then_ThrowsAfterMaxAttempts()
    {
        var factory = new FailThenSucceedFactory(failCount: 100, null!);

        await using var reader = new UartSerialReader(factory, DefaultConfig,
            NullLogger<UartSerialReader>.Instance);

        var act = async () =>
        {
            await foreach (var _ in reader.ReadAsync(TestContext.Current.CancellationToken))
            { }
        };

        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task Given_Cancellation_When_ReadAsync_Then_ExitsCleanly()
    {
        // Port that blocks on ReadLineAsync until cancellation
        var blockingPort = new BlockingSerialPort();
        var factory = new FakeSerialPortFactory(new Queue<ISerialPort>([blockingPort]));

        await using var reader = new UartSerialReader(factory, DefaultConfig,
            NullLogger<UartSerialReader>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = async () =>
        {
            await foreach (var _ in reader.ReadAsync(cts.Token))
            { }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Given_IoExceptionDuringRead_When_ReadAsync_Then_ReconnectsWithNewPort()
    {
        var failingPort = new FakeSerialPort(
            new Queue<string?>(["INFO: first"]),
            throwAfter: new IOException("Device disconnected"));

        var recoveryLines = new Queue<string?>(["INFO: recovered", null]);
        var recoveryPort = new FakeSerialPort(recoveryLines);

        var factory = new FakeSerialPortFactory(
            new Queue<ISerialPort>([failingPort, recoveryPort]));

        await using var reader = new UartSerialReader(factory, DefaultConfig,
            NullLogger<UartSerialReader>.Instance);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().HaveCount(2);
        entries[0].RawLine.Should().Be("INFO: first");
        entries[1].RawLine.Should().Be("INFO: recovered");
    }
}
