// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SerialSavant.Config;

namespace SerialSavant.Tests;

public sealed class AppSettingsRepositoryTests
{
    // ── helpers ──────────────────────────────────────────────────────────

    private static string TempConfigPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        return Path.Combine(dir, "config.json");
    }

    private static AppSettingsRepository MakeRepository(string path) =>
        new(NullLogger<AppSettingsRepository>.Instance, path);

    // ── CreateDefault tests ───────────────────────────────────────────────

    [Fact]
    public void CreateDefault_Serial_PortIsNotEmpty()
    {
        var settings = AppSettings.CreateDefault();

        settings.Serial.Port.Should().NotBeNullOrWhiteSpace();
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

    // ── load tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaultsWithWasDefaultedTrue()
    {
        var repo = MakeRepository(TempConfigPath());

        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeTrue();
        settings.Serial.Port.Should().NotBeNullOrWhiteSpace();
        settings.Serial.BaudRate.Should().Be(115200);
    }

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsDeserializedSettings()
    {
        var path = TempConfigPath();
        // Path.GetDirectoryName is non-null here: path always has a directory segment.
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        const string json = """
            {
              "Serial": { "Port": "/dev/ttyACM0", "BaudRate": 9600 },
              "Llm": { "ModelPath": "/models/llama.gguf", "MaxTokens": 256,
                       "Temperature": 0.5, "ServerPort": 8080, "TimeoutMs": 5000 }
            }
            """;
        await File.WriteAllTextAsync(path, json);

        var repo = MakeRepository(path);
        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeFalse();
        settings.Serial.Port.Should().Be("/dev/ttyACM0");
        settings.Serial.BaudRate.Should().Be(9600);
        settings.Llm.ModelPath.Should().Be("/models/llama.gguf");
    }

    [Fact]
    public async Task LoadAsync_CorruptFile_ReturnsDefaultsWithWasDefaultedTrue()
    {
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "this is not json {{{{");

        var repo = MakeRepository(path);
        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeTrue();
        settings.Serial.BaudRate.Should().Be(115200);
    }

    [Fact]
    public async Task LoadAsync_NullJsonFile_ReturnsDefaultsWithWasDefaultedTrue()
    {
        // STJ deserializes the JSON literal "null" as null for reference types,
        // triggering the null-result guard in LoadAsync.
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "null");

        var repo = MakeRepository(path);
        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeTrue();
        settings.Serial.BaudRate.Should().Be(115200);
    }

    [Fact]
    public async Task LoadAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{}");

        var repo = MakeRepository(path);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await repo.Invoking(r => r.LoadAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
