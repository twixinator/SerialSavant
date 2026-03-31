// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Core;

namespace SerialSavant.Tests;

public sealed class AnalysisResultTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsInstance()
    {
        var result = AnalysisResult.Create("explanation", Severity.High, ["do something"]);

        result.Explanation.Should().Be("explanation");
        result.Severity.Should().Be(Severity.High);
        result.Suggestions.Should().ContainSingle().Which.Should().Be("do something");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceExplanation_Throws(string explanation)
    {
        var act = () => AnalysisResult.Create(explanation, Severity.High, ["suggestion"]);

        act.Should().Throw<ArgumentException>().WithParameterName("explanation");
    }

    [Fact]
    public void Create_UnknownSeverity_Throws()
    {
        var act = () => AnalysisResult.Create("explanation", Severity.Unknown, ["suggestion"]);

        act.Should().Throw<ArgumentException>().WithParameterName("severity");
    }

    [Fact]
    public void Create_EmptySuggestions_Throws()
    {
        var act = () => AnalysisResult.Create("explanation", Severity.High, []);

        act.Should().Throw<ArgumentException>().WithParameterName("suggestions");
    }

    [Fact]
    public void Create_NullSuggestions_Throws()
    {
        var act = () => AnalysisResult.Create("explanation", Severity.High, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("suggestions");
    }
}
