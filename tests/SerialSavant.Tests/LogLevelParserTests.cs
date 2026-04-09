using AwesomeAssertions;
using SerialSavant.Core;

namespace SerialSavant.Tests;

public sealed class LogLevelParserTests
{
    [Theory]
    [InlineData("FATAL: something crashed", SerialLogLevel.Fatal)]
    [InlineData("ERROR: ENOMEM - Cannot allocate memory", SerialLogLevel.Error)]
    [InlineData("WARNING: low battery", SerialLogLevel.Warning)]
    [InlineData("WARN: temperature high", SerialLogLevel.Warning)]
    [InlineData("INFO: boot complete", SerialLogLevel.Info)]
    [InlineData("DEBUG: entering main()", SerialLogLevel.Debug)]
    public void Given_LineWithKnownPrefix_When_Parse_Then_ReturnsExpectedLevel(
        string rawLine, SerialLogLevel expected)
    {
        LogLevelParser.Parse(rawLine).Should().Be(expected);
    }

    [Theory]
    [InlineData("0x00 0x1A 0x2F")]
    [InlineData("#0 0x0800ABCD in main()")]
    [InlineData("")]
    [InlineData("some random text")]
    public void Given_LineWithNoKnownPrefix_When_Parse_Then_ReturnsUnknown(string rawLine)
    {
        LogLevelParser.Parse(rawLine).Should().Be(SerialLogLevel.Unknown);
    }

    [Theory]
    [InlineData("fatal: lowercase works")]
    [InlineData("Error: mixed case")]
    [InlineData("  INFO: leading whitespace")]
    public void Given_LineWithVariantCasing_When_Parse_Then_MatchesCaseInsensitively(string rawLine)
    {
        LogLevelParser.Parse(rawLine).Should().NotBe(SerialLogLevel.Unknown);
    }
}
