namespace SerialSavant.Core;

/// <summary>
/// Parses a <see cref="SerialLogLevel"/> from the prefix of a raw serial line.
/// </summary>
public static class LogLevelParser
{
    private static readonly (string Prefix, SerialLogLevel Level)[] Prefixes =
    [
        ("FATAL:", SerialLogLevel.Fatal),
        ("ERROR:", SerialLogLevel.Error),
        ("WARNING:", SerialLogLevel.Warning),
        ("WARN:", SerialLogLevel.Warning),
        ("INFO:", SerialLogLevel.Info),
        ("DEBUG:", SerialLogLevel.Debug),
    ];

    public static SerialLogLevel Parse(string rawLine)
    {
        var trimmed = rawLine.AsSpan().TrimStart();

        foreach (var (prefix, level) in Prefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return level;
        }

        return SerialLogLevel.Unknown;
    }
}
