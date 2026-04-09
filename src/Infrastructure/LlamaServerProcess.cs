using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SerialSavant.Config;

namespace SerialSavant.Infrastructure;

public sealed class LlamaServerProcess : BackgroundService, ILlamaServerGate
{
    private readonly LlmConfig _config;
    private readonly ILogger<LlamaServerProcess> _logger;
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _crashed;
    private Process? _process;

    public LlamaServerProcess(
        IOptions<LlmConfig> options,
        ILogger<LlamaServerProcess> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    public Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (_crashed)
            throw new InvalidOperationException("llama-server process has exited unexpectedly.");

        cancellationToken.ThrowIfCancellationRequested();

        return _readyTcs.Task.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _process = StartProcess();
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    _logger.LogDebug("[llama-server stdout] {Line}", e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    _logger.LogWarning("[llama-server stderr] {Line}", e.Data);
            };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await PollHealthAsync(stoppingToken).ConfigureAwait(false);

            _readyTcs.TrySetResult();
            _logger.LogInformation("llama-server is ready on port {Port}", _config.ServerPort);

            // Stay alive monitoring until shutdown requested
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _readyTcs.TrySetException(ex);
            _logger.LogError(ex, "llama-server failed to start");
        }
    }

    private Process StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _config.ServerPath,
            Arguments = $"--model \"{_config.ModelPath}\" --port {_config.ServerPort} --host 127.0.0.1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _logger.LogInformation(
            "Starting llama-server: {Path} {Args}", startInfo.FileName, startInfo.Arguments);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {_config.ServerPath}");
    }

    private async Task PollHealthAsync(CancellationToken cancellationToken)
    {
        // Single HttpClient for the health poll loop — disposed when polling completes.
        // Not using IHttpClientFactory here because this runs during host startup
        // before the DI container is fully built. The client is short-lived and
        // disposed deterministically, so socket exhaustion is not a concern.
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        var healthUrl = $"http://127.0.0.1:{_config.ServerPort}/health";
        var deadline = DateTime.UtcNow.AddMilliseconds(_config.TimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await client.GetAsync(healthUrl, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"llama-server did not become healthy within {_config.TimeoutMs}ms.");
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _crashed = true;
        var exitCode = _process?.ExitCode;
        _logger.LogError("llama-server exited unexpectedly with code {ExitCode}", exitCode);
        _readyTcs.TrySetException(
            new InvalidOperationException($"llama-server exited with code {exitCode}."));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        if (_process is { HasExited: false } proc)
        {
            _logger.LogInformation("Stopping llama-server");
            try
            {
                proc.Kill(entireProcessTree: true);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await proc.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error while stopping llama-server");
            }
        }

        _process?.Dispose();
        _process = null;
    }

    // Test helpers — internal for unit test access
    internal void SimulateReady() => _readyTcs.TrySetResult();

    internal void SimulateCrash()
    {
        _crashed = true;
        _readyTcs.TrySetException(
            new InvalidOperationException("llama-server exited unexpectedly (simulated)."));
    }
}
