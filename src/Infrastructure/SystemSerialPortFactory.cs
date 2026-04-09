namespace SerialSavant.Infrastructure;

public sealed class SystemSerialPortFactory : ISerialPortFactory
{
    public ISerialPort Create(string portName, int baudRate) =>
        new SystemSerialPort(portName, baudRate);
}
