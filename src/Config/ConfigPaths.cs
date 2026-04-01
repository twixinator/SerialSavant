// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Config;

public static class ConfigPaths
{
    private const string ConfigDirName = ".serialsavant";
    private const string ConfigFileName = "config.json";

    public static readonly string ConfigFilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ConfigDirName,
            ConfigFileName);
}
