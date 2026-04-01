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
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);

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
        await File.WriteAllTextAsync(path, "this is not json {{{{", TestContext.Current.CancellationToken);

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
        await File.WriteAllTextAsync(path, "null", TestContext.Current.CancellationToken);

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
        await File.WriteAllTextAsync(path, "{}", TestContext.Current.CancellationToken);

        var repo = MakeRepository(path);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await repo.Invoking(r => r.LoadAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAsync_MissingFileInExistingDirectory_ReturnsDefaultsWithWasDefaultedTrue()
    {
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Directory exists but file does not — triggers FileNotFoundException

        var repo = MakeRepository(path);
        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeTrue();
        settings.Serial.BaudRate.Should().Be(115200);
    }

    [Fact]
    public async Task LoadAsync_IOException_Propagates()
    {
        var path = TempConfigPath();
        // Create a directory at the file path — ReadAllTextAsync will fail (EISDIR/access denied)
        Directory.CreateDirectory(path);

        var repo = MakeRepository(path);
        await repo.Invoking(r => r.LoadAsync(TestContext.Current.CancellationToken))
            .Should().ThrowAsync<Exception>()
            .Where(e => e is IOException || e is UnauthorizedAccessException);
    }

    // ── save tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_CreatesDirectoryAndFile()
    {
        var path = TempConfigPath();
        var repo = MakeRepository(path);
        var settings = AppSettings.CreateDefault() with
        {
            Serial = new SerialConfig { Port = "/dev/ttyUSB1", BaudRate = 57600 },
        };

        await repo.SaveAsync(settings, TestContext.Current.CancellationToken);

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "old content", TestContext.Current.CancellationToken);

        var repo = MakeRepository(path);
        await repo.SaveAsync(AppSettings.CreateDefault(), TestContext.Current.CancellationToken);

        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        content.Should().NotBe("old content");
        content.Should().Contain("115200");
    }

    [Fact]
    public async Task SaveAsync_RoundTrip_LoadReturnsEquivalentSettings()
    {
        var path = TempConfigPath();
        var repo = MakeRepository(path);
        var original = AppSettings.CreateDefault() with
        {
            Serial = new SerialConfig { Port = "/dev/ttyACM0", BaudRate = 9600 },
            Llm = new LlmConfig { ModelPath = "/models/llama.gguf" },
        };

        await repo.SaveAsync(original, TestContext.Current.CancellationToken);
        var (loaded, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeFalse();
        loaded.Should().Be(original);
    }

    [Fact]
    public async Task SaveAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var path = TempConfigPath();
        var repo = MakeRepository(path);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await repo.Invoking(r => r.SaveAsync(AppSettings.CreateDefault(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveAsync_NullSettings_ThrowsArgumentNullException()
    {
        var repo = MakeRepository(TempConfigPath());

        await repo.Invoking(r => r.SaveAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SaveAsync_IoException_PropagatesException()
    {
        var path = TempConfigPath();
        // Create a directory at the file path — WriteAllTextAsync will fail
        Directory.CreateDirectory(path);

        var repo = MakeRepository(path);
        await repo.Invoking(r => r.SaveAsync(AppSettings.CreateDefault(), TestContext.Current.CancellationToken))
            .Should().ThrowAsync<Exception>()
            .Where(e => e is IOException || e is UnauthorizedAccessException);
    }

    // ── constructor tests ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AppSettingsRepository(null!, TempConfigPath());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LoadAsync_PartialJsonFile_MissingOptionalFieldsGetClrDefaults()
    {
        var path = TempConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // STJ source-generated deserialization of records with init properties does not apply
        // C# property initializer defaults for absent fields — missing numeric fields become 0.
        const string json = """
            {
              "Serial": { "Port": "/dev/ttyACM0", "BaudRate": 9600 },
              "Llm": { "ModelPath": "/models/llama.gguf" }
            }
            """;
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);

        var repo = MakeRepository(path);
        var (settings, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeFalse();
        settings.Serial.Port.Should().Be("/dev/ttyACM0");
        settings.Serial.BaudRate.Should().Be(9600);
        settings.Serial.ReconnectBaseDelayMs.Should().Be(0);
        settings.Serial.ReconnectMaxDelayMs.Should().Be(0);
        settings.Serial.ReconnectMaxAttempts.Should().Be(0);
        settings.Llm.ModelPath.Should().Be("/models/llama.gguf");
        settings.Llm.MaxTokens.Should().Be(0);
        settings.Llm.Temperature.Should().Be(0f);
        settings.Llm.ServerPort.Should().Be(0);
        settings.Llm.TimeoutMs.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_RoundTrip_AllLlmFieldsPreserved()
    {
        var path = TempConfigPath();
        var repo = MakeRepository(path);
        var original = AppSettings.CreateDefault() with
        {
            Llm = new LlmConfig
            {
                ModelPath = "/models/llama.gguf",
                MaxTokens = 256,
                Temperature = 0.3f,
                ServerPort = 9090,
                TimeoutMs = 15_000,
            },
        };

        await repo.SaveAsync(original, TestContext.Current.CancellationToken);
        var (loaded, wasDefaulted) = await repo.LoadAsync(TestContext.Current.CancellationToken);

        wasDefaulted.Should().BeFalse();
        loaded.Should().Be(original);
    }
}
