// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Config;

namespace SerialSavant.Tests;

public sealed class AppSettingsRepositoryTests
{
    [Fact]
    public void CreateDefault_Serial_PortIsNotEmpty()
    {
        var settings = AppSettings.CreateDefault();

        settings.Serial.Port.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateDefault_Serial_BaudRateIs115200()
    {
        var settings = AppSettings.CreateDefault();

        settings.Serial.BaudRate.Should().Be(115200);
    }

    [Fact]
    public void CreateDefault_Llm_ModelPathIsEmpty()
    {
        var settings = AppSettings.CreateDefault();

        settings.Llm.ModelPath.Should().BeEmpty();
    }
}
