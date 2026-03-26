namespace SerialSavant.Core;

public interface ISerialReader : IAsyncDisposable
{
    IAsyncEnumerable<LogEntry> ReadAsync(CancellationToken cancellationToken = default);
}
