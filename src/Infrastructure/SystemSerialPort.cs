// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SerialSavant.Infrastructure;

/// <summary>
/// Production wrapper over <see cref="SerialPort"/>. Reads via
/// <see cref="SerialPort.BaseStream"/> with manual line buffering
/// for genuine async + cancellation support.
/// </summary>
public sealed class SystemSerialPort : ISerialPort
{
    private readonly SerialPort _port;
    private readonly ILogger<SystemSerialPort> _logger;
    private readonly byte[] _readBuffer = new byte[1024];
    private readonly StringBuilder _lineBuffer = new();
    private bool _disposed;

    public SystemSerialPort(string portName, int baudRate, ILogger<SystemSerialPort> logger)
    {
        _logger = logger;
        _port = new SerialPort(portName, baudRate)
        {
            ReadTimeout = SerialPort.InfiniteTimeout,
            NewLine = "\n"
        };
    }

    public bool IsOpen => !_disposed && _port.IsOpen;

    public void Open()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _port.Open();
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Scan existing buffer for a newline without allocating ToString().
            for (var i = 0; i < _lineBuffer.Length; i++)
            {
                if (_lineBuffer[i] == '\n')
                {
                    var lineLength = (i > 0 && _lineBuffer[i - 1] == '\r') ? i - 1 : i;
                    var line = _lineBuffer.ToString(0, lineLength);
                    _lineBuffer.Remove(0, i + 1);
                    return line;
                }
            }

            var bytesRead = await _port.BaseStream.ReadAsync(
                _readBuffer, cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
                return null;

            _lineBuffer.Append(Encoding.UTF8.GetString(_readBuffer, 0, bytesRead));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_port.IsOpen)
        {
            try { _port.Close(); }
            catch (IOException ex)
            {
                _logger.LogDebug(ex, "IOException during serial port close on {PortName}", _port.PortName);
            }
        }

        _port.Dispose();
    }
}
