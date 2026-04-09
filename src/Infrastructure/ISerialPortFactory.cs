namespace SerialSavant.Infrastructure;

/// <summary>
/// Creates fresh <see cref="ISerialPort"/> instances.
/// Each reconnect attempt should request a new instance.
/// </summary>
public interface ISerialPortFactory
{
    ISerialPort Create(string portName, int baudRate);
}
