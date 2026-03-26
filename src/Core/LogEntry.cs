namespace SerialSavant.Core;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string RawLine,
    SerialLogLevel LogLevel);
