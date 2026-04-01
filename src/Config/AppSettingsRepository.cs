// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SerialSavant.Config;

public sealed class AppSettingsRepository(
    ILogger<AppSettingsRepository> logger,
    string? configFilePath = null)
{
    private readonly ILogger<AppSettingsRepository> _logger = logger;
    private readonly string _configFilePath = configFilePath ?? ConfigPaths.ConfigFilePath;

    public async Task<(AppSettings Settings, bool WasDefaulted)> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation("Config file not found at {Path}, using defaults", _configFilePath);
            return (AppSettings.CreateDefault(), true);
        }

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
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Config file at {Path} is corrupt, using defaults", _configFilePath);
            return (AppSettings.CreateDefault(), true);
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
