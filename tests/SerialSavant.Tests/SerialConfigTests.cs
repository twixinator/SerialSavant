// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Config;

namespace SerialSavant.Tests;

public sealed class SerialConfigTests
{
    [Fact]
    public void SerialConfig_DefaultConstructor_PortIsNotEmpty()
    {
        var config = new SerialConfig();

        config.Port.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SerialConfig_DefaultConstructor_BaudRateIs115200()
    {
        var config = new SerialConfig();

        config.BaudRate.Should().Be(115200);
    }
}
