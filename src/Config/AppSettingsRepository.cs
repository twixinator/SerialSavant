// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SerialSavant.Config;

public sealed class AppSettingsRepository(
    ILogger<AppSettingsRepository> logger,
    string? configFilePath = null)
{
    private readonly ILogger<AppSettingsRepository> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _configFilePath =
        string.IsNullOrWhiteSpace(configFilePath ?? ConfigPaths.ConfigFilePath)
            ? throw new ArgumentException("Config file path must not be empty.", nameof(configFilePath))
            : (configFilePath ?? ConfigPaths.ConfigFilePath);

    /// <summary>
    /// Loads <see cref="AppSettings"/> from the config file asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token; <see cref="OperationCanceledException"/> propagates normally.</param>
    /// <returns>
    /// A tuple of the loaded settings and a flag indicating whether defaults were used.
    /// <c>WasDefaulted</c> is <see langword="true"/> when the file is absent, unreadable as JSON,
    /// or produces a <see langword="null"/> deserialisation result.
    /// It is <see langword="false"/> only when a valid, non-null settings object was successfully loaded.
    /// </returns>
    /// <exception cref="IOException">The file exists but could not be read due to an I/O error.</exception>
    /// <exception cref="UnauthorizedAccessException">The file exists but the process lacks read permission.</exception>
    public async Task<(AppSettings Settings, bool WasDefaulted)> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath, ct).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);

            if (settings is null)
            {
                _logger.LogWarning("Config file at {Path} deserialized to null, using defaults", _configFilePath);
                return (AppSettings.CreateDefault(), true);
            }

            return (settings, false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogInformation("Config file not found at {Path}, using defaults", _configFilePath);
            return (AppSettings.CreateDefault(), true);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Config file at {Path} is corrupt, using defaults", _configFilePath);
            return (AppSettings.CreateDefault(), true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to read config at {Path}", _configFilePath);
            throw;
        }
    }

    /// <summary>
    /// Saves <paramref name="settings"/> to the config file atomically.
    /// </summary>
    /// <remarks>
    /// The file is written to a <c>.tmp</c> sibling first, then renamed to the final path,
    /// ensuring the config is never left in a partially-written state.
    /// </remarks>
    /// <param name="settings">The settings to persist; must not be <see langword="null"/>.</param>
    /// <param name="ct">Cancellation token; <see cref="OperationCanceledException"/> propagates without logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Serialization of <paramref name="settings"/> failed.</exception>
    /// <exception cref="IOException">The file could not be written due to an I/O error.</exception>
    /// <exception cref="UnauthorizedAccessException">The process lacks write permission to the config directory.</exception>
    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_configFilePath);
        if (directory is null)
        {
            _logger.LogError("Cannot determine parent directory for config path {Path}", _configFilePath);
            throw new InvalidOperationException(
                $"Cannot determine directory for config path '{_configFilePath}'.");
        }

        _logger.LogDebug("Saving config to {Path}", _configFilePath);

        var tempPath = _configFilePath + ".tmp";

        try
        {
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, _configFilePath, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            // Must precede IOException filter: propagate cancellation without logging.
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize settings for config path {Path}", _configFilePath);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to save config to {Path}", _configFilePath);
            throw;
        }
    }
}
