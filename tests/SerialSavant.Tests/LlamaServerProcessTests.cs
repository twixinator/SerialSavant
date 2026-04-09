using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SerialSavant.Config;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class LlamaServerProcessTests
{
    private static IOptions<LlmConfig> CreateOptions(LlmConfig? config = null) =>
        Options.Create(config ?? new LlmConfig
        {
            ModelPath = "test-model.gguf",
            ServerPort = 0, // will not actually start
            TimeoutMs = 1000,
            ServerPath = "llama-server"
        });

    [Fact]
    public async Task Given_ReadyGate_When_WaitForReadyAsyncCalledAfterReady_Then_ReturnsImmediately()
    {
        var process = new LlamaServerProcess(
            CreateOptions(), NullLogger<LlamaServerProcess>.Instance);

        // Simulate readiness by completing the TCS externally via test helper
        process.SimulateReady();

        var task = process.WaitForReadyAsync(CancellationToken.None);
        task.IsCompleted.Should().BeTrue();
        await task; // should not throw
    }

    [Fact]
    public async Task Given_CrashedServer_When_WaitForReadyAsync_Then_ThrowsInvalidOperationException()
    {
        var process = new LlamaServerProcess(
            CreateOptions(), NullLogger<LlamaServerProcess>.Instance);

        process.SimulateCrash();

        var act = () => process.WaitForReadyAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Given_CancelledToken_When_WaitForReadyAsync_Then_ThrowsOperationCancelled()
    {
        var process = new LlamaServerProcess(
            CreateOptions(), NullLogger<LlamaServerProcess>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => process.WaitForReadyAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
