// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerialSavant.Core;

namespace SerialSavant.UI;

/// <summary>
/// Hosted service that drives the read → analyze → render loop.
/// Reads log entries from <see cref="ISerialReader"/>, analyzes each via
/// <see cref="ILlmAnalyzer"/>, and renders results via <see cref="ILogRenderer"/>.
/// Stops the application host when the stream ends or cancellation is requested.
/// </summary>
public sealed class ConsoleOrchestrator(
    ISerialReader reader,
    ILlmAnalyzer analyzer,
    ILogRenderer renderer,
    IHostApplicationLifetime lifetime,
    ILogger<ConsoleOrchestrator> logger) : IHostedService
{
    private readonly ISerialReader _reader = reader;
    private readonly ILlmAnalyzer _analyzer = analyzer;
    private readonly ILogRenderer _renderer = renderer;
    private readonly IHostApplicationLifetime _lifetime = lifetime;
    private readonly ILogger<ConsoleOrchestrator> _logger = logger;

    private Task _executeTask = Task.CompletedTask;
    private CancellationTokenSource? _cts;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executeTask = ExecuteAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            await _executeTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
        {
            // Expected when the host stop timeout fires before ExecuteAsync finishes.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _renderer.RenderHeader();
        var count = 0;

        try
        {
            await foreach (var entry in _reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                AnalysisResult result;
                try
                {
                    result = await _analyzer.AnalyzeAsync(entry, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Analysis failed for log entry {RawLine}", entry.RawLine);
                    continue;
                }

                _renderer.Render(entry, result);
                count++;
            }

            _renderer.RenderSummary(count);
            _logger.LogInformation("Session complete after {Count} entries", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Session stopped after {Count} entries", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in session after {Count} entries", count);
            Environment.ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
