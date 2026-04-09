using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SerialSavant.Config;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class UartSerialReader(
    ISerialPortFactory factory,
    SerialConfig config,
    ILogger<UartSerialReader> logger) : ISerialReader
{
    private ISerialPort? _currentPort;

    public async IAsyncEnumerable<LogEntry> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _currentPort = await ConnectAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? line;
            try
            {
                line = await _currentPort.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Serial read failed on {Port}, attempting reconnect", config.Port);
                _currentPort.Dispose();
                _currentPort = await ConnectAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (line is null)
                yield break;

            var level = LogLevelParser.Parse(line);
            yield return new LogEntry(DateTimeOffset.UtcNow, line, level);
        }
    }

    private async Task<ISerialPort> ConnectAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < config.ReconnectMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (attempt > 0)
            {
                var shift = Math.Min(attempt, 30);
                var delayMs = Math.Min(
                    config.ReconnectBaseDelayMs * (1 << shift),
                    config.ReconnectMaxDelayMs);

                logger.LogInformation(
                    "Reconnecting to {Port}, attempt {Attempt}/{Max}, delay {DelayMs}ms",
                    config.Port, attempt + 1, config.ReconnectMaxAttempts, delayMs);

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var port = factory.Create(config.Port, config.BaudRate);
                port.Open();

                logger.LogInformation("Connected to {Port} at {BaudRate} baud",
                    config.Port, config.BaudRate);

                return port;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                logger.LogWarning(ex,
                    "Failed to open {Port}, attempt {Attempt}/{Max}",
                    config.Port, attempt + 1, config.ReconnectMaxAttempts);
            }
        }

        throw new IOException(
            $"Failed to connect to {config.Port} after {config.ReconnectMaxAttempts} attempts.");
    }

    public ValueTask DisposeAsync()
    {
        _currentPort?.Dispose();
        _currentPort = null;
        return ValueTask.CompletedTask;
    }
}
