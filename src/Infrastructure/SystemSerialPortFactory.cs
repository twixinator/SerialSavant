// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace SerialSavant.Infrastructure;

public sealed class SystemSerialPortFactory(ILoggerFactory loggerFactory) : ISerialPortFactory
{
    public ISerialPort Create(string portName, int baudRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        return new SystemSerialPort(portName, baudRate, loggerFactory.CreateLogger<SystemSerialPort>());
    }
}
