// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

// SerialSavant - UART Log Analyzer with Local LLM
// Run:           dotnet run -- [--mock]
// Publish (AOT): dotnet publish -r win-x64 -c Release

var isMockMode = args.Contains("--mock", StringComparer.OrdinalIgnoreCase);

if (isMockMode)
{
    Console.Error.WriteLine(
        "[WARNING] Mock mode active: MockSerialReader and MockLlmAnalyzer produce " +
        "fabricated data only. Do not use in production.");
}

Console.WriteLine("SerialSavant v0.1.0 - initializing...");
