# Mock Implementations & Unit Tests — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add MockSerialReader and MockLlmAnalyzer with full TDD coverage, plus the xUnit test project infrastructure.

**Architecture:** Two mock classes in `src/Infrastructure/` implementing existing `ISerialReader` and `ILlmAnalyzer` interfaces from `Core`. A new xUnit test project at `tests/SerialSavant.Tests/` with AwesomeAssertions. TDD throughout — tests written before implementations.

**Tech Stack:** .NET 10 / C# 14, xUnit, AwesomeAssertions, Native AOT compatible

---

## File Map

| Action | File | Responsibility |
|--------|------|---------------|
| Create | `tests/SerialSavant.Tests/SerialSavant.Tests.csproj` | Test project config |
| Create | `tests/SerialSavant.Tests/MockSerialReaderTests.cs` | Tests for MockSerialReader |
| Create | `tests/SerialSavant.Tests/MockLlmAnalyzerTests.cs` | Tests for MockLlmAnalyzer |
| Create | `src/Infrastructure/MockMode.cs` | MockMode enum |
| Create | `src/Infrastructure/MockSerialReader.cs` | ISerialReader mock with deterministic + random modes |
| Create | `src/Infrastructure/MockLlmAnalyzer.cs` | ILlmAnalyzer mock with content-based heuristics |
| Delete | `src/Infrastructure/Placeholder.cs` | No longer needed after real files exist |
| Modify | `SerialSavant.sln` | Add test project to solution |

---

### Task 1: Scaffold the xUnit test project

**Files:**
- Create: `tests/SerialSavant.Tests/SerialSavant.Tests.csproj`
- Modify: `SerialSavant.sln`

- [ ] **Step 1: Create the xUnit project**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
mkdir -p tests/SerialSavant.Tests
dotnet new xunit --framework net10.0 -o tests/SerialSavant.Tests --force
```

- [ ] **Step 2: Add project references and packages**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet add tests/SerialSavant.Tests reference src/Core/SerialSavant.Core.csproj
dotnet add tests/SerialSavant.Tests reference src/Infrastructure/SerialSavant.Infrastructure.csproj
dotnet add tests/SerialSavant.Tests package AwesomeAssertions
```

- [ ] **Step 3: Configure the .csproj**

Edit `tests/SerialSavant.Tests/SerialSavant.Tests.csproj` — ensure these properties are set in the `<PropertyGroup>`:

```xml
<IsAotCompatible>false</IsAotCompatible>
```

Remove any auto-generated `UnitTest1.cs` file if created by the template.

- [ ] **Step 4: Add test project to the solution**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet sln add tests/SerialSavant.Tests/SerialSavant.Tests.csproj
```

- [ ] **Step 5: Verify the setup**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet build
dotnet test --logger "console;verbosity=detailed"
```

Expected: Build succeeds. `dotnet test` runs with 0 tests, exit code 0.

- [ ] **Step 6: Commit**

```bash
git add -A tests/ SerialSavant.sln
git commit -m "chore: scaffold xUnit test project with AwesomeAssertions

Closes #6"
```

---

### Task 2: MockMode enum and MockSerialReader skeleton

**Files:**
- Create: `src/Infrastructure/MockMode.cs`
- Create: `src/Infrastructure/MockSerialReader.cs`
- Delete: `src/Infrastructure/Placeholder.cs`

- [ ] **Step 1: Create MockMode enum**

Create `src/Infrastructure/MockMode.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

namespace SerialSavant.Infrastructure;

public enum MockMode
{
    Deterministic,
    Random
}
```

- [ ] **Step 2: Create MockSerialReader with minimal skeleton**

Create `src/Infrastructure/MockSerialReader.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class MockSerialReader(
    MockMode mode,
    TimeSpan? delay = null,
    TimeProvider? timeProvider = null,
    Random? random = null) : ISerialReader
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Random _random = random ?? new Random();
    private readonly TimeSpan? _delay = delay;
    private readonly MockMode _mode = mode;

    public async IAsyncEnumerable<LogEntry> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Minimal: will be implemented in TDD steps
        await Task.CompletedTask;
        yield break;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

- [ ] **Step 3: Delete Placeholder.cs**

Delete `src/Infrastructure/Placeholder.cs`.

- [ ] **Step 4: Verify build**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add -u src/Infrastructure/
git add src/Infrastructure/MockMode.cs src/Infrastructure/MockSerialReader.cs
git commit -m "feat(infra): add MockMode enum and MockSerialReader skeleton"
```

---

### Task 3: MockSerialReader — deterministic mode (TDD)

**Files:**
- Create: `tests/SerialSavant.Tests/MockSerialReaderTests.cs`
- Modify: `src/Infrastructure/MockSerialReader.cs`

- [ ] **Step 1: Write failing tests for deterministic mode**

Create `tests/SerialSavant.Tests/MockSerialReaderTests.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class MockSerialReaderTests
{
    [Fact]
    public async Task ReadAsync_DeterministicMode_ReturnsExpectedSequence()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync())
        {
            entries.Add(entry);
        }

        entries.Should().NotBeEmpty();
        entries.Should().HaveCount(10);
    }

    [Fact]
    public async Task ReadAsync_DeterministicMode_AllLogCategoriesPresent()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var entries = new List<LogEntry>();
        await foreach (var entry in reader.ReadAsync())
        {
            entries.Add(entry);
        }

        var rawLines = entries.Select(e => e.RawLine).ToList();

        // Hex dump: contains "0x" byte patterns
        rawLines.Should().Contain(line => line.Contains("0x", StringComparison.Ordinal));

        // C errno: contains known errno names
        rawLines.Should().Contain(line =>
            line.Contains("ENOMEM", StringComparison.Ordinal) ||
            line.Contains("SIGSEGV", StringComparison.Ordinal) ||
            line.Contains("EACCES", StringComparison.Ordinal));

        // Stack trace: contains frame markers
        rawLines.Should().Contain(line =>
            line.Contains("#0", StringComparison.Ordinal) ||
            line.Contains("#1", StringComparison.Ordinal) ||
            line.Contains("at 0x", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadAsync_DeterministicMode_CompletesAfterFullSequence()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var count = 0;

        await foreach (var _ in reader.ReadAsync(cts.Token))
        {
            count++;
        }

        // Enumeration completed naturally (not via cancellation timeout)
        cts.IsCancellationRequested.Should().BeFalse();
        count.Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockSerialReaderTests"
```

Expected: All 3 tests FAIL (reader yields nothing via `yield break`).

- [ ] **Step 3: Implement deterministic mode**

Replace the `ReadAsync` method body and add the deterministic entries in `src/Infrastructure/MockSerialReader.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class MockSerialReader(
    MockMode mode,
    TimeSpan? delay = null,
    TimeProvider? timeProvider = null,
    Random? random = null) : ISerialReader
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Random _random = random ?? new Random();
    private readonly TimeSpan? _delay = delay;
    private readonly MockMode _mode = mode;

    private static readonly (string RawLine, SerialLogLevel Level)[] DeterministicEntries =
    [
        ("0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD", SerialLogLevel.Debug),
        ("0xCA 0xFE 0xBA 0xBE 0x00 0x00 0x01", SerialLogLevel.Debug),
        ("0x7F 0x45 0x4C 0x46 0x02 0x01 0x01", SerialLogLevel.Info),
        ("ERROR: ENOMEM - Cannot allocate memory", SerialLogLevel.Error),
        ("ERROR: EACCES - Permission denied", SerialLogLevel.Error),
        ("FATAL: SIGSEGV - Segmentation fault at 0x00000010", SerialLogLevel.Fatal),
        ("FATAL: SIGBUS - Bus error at 0x0000DEAD", SerialLogLevel.Fatal),
        ("#0 0x0800ABCD in main() at firmware.c:142", SerialLogLevel.Error),
        ("#1 0x0800EF01 in init_hardware() at hal.c:87", SerialLogLevel.Error),
        ("#2 0x08001234 in reset_handler() at startup.s:12", SerialLogLevel.Warning),
    ];

    public async IAsyncEnumerable<LogEntry> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_mode is MockMode.Deterministic)
        {
            foreach (var (rawLine, level) in DeterministicEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new LogEntry(_timeProvider.GetUtcNow(), rawLine, level);

                if (_delay is { } d)
                {
                    await Task.Delay(d, cancellationToken);
                }
            }
        }
        else
        {
            // Random mode — implemented in Task 4
            await Task.CompletedTask;
            yield break;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockSerialReaderTests"
```

Expected: All 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/SerialSavant.Tests/MockSerialReaderTests.cs src/Infrastructure/MockSerialReader.cs
git commit -m "feat(infra): implement MockSerialReader deterministic mode with TDD"
```

---

### Task 4: MockSerialReader — random mode, cancellation, and dispose (TDD)

**Files:**
- Modify: `tests/SerialSavant.Tests/MockSerialReaderTests.cs`
- Modify: `src/Infrastructure/MockSerialReader.cs`

- [ ] **Step 1: Write failing tests for random mode, cancellation, and dispose**

Append these tests to `tests/SerialSavant.Tests/MockSerialReaderTests.cs`:

```csharp
    [Fact]
    public async Task ReadAsync_RandomMode_RespectsDelayBetweenEntries()
    {
        var delay = TimeSpan.FromMilliseconds(50);
        await using var reader = new MockSerialReader(
            MockMode.Random,
            delay: delay,
            random: new Random(42));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;

        await foreach (var _ in reader.ReadAsync(cts.Token))
        {
            count++;
            if (count >= 3)
            {
                break;
            }
        }

        stopwatch.Stop();

        // 3 entries with 50ms delay each = at least ~100ms (delays between entries, not before first)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public async Task ReadAsync_CancellationRequested_StopsEmitting()
    {
        await using var reader = new MockSerialReader(
            MockMode.Random,
            random: new Random(42));

        using var cts = new CancellationTokenSource();
        var count = 0;

        await foreach (var _ in reader.ReadAsync(cts.Token))
        {
            count++;
            if (count >= 5)
            {
                cts.Cancel();
                break;
            }
        }

        count.Should().Be(5);
    }

    [Fact]
    public async Task ReadAsync_PreCancelledToken_StopsImmediately()
    {
        await using var reader = new MockSerialReader(MockMode.Deterministic);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var entries = new List<LogEntry>();

        var act = async () =>
        {
            await foreach (var entry in reader.ReadAsync(cts.Token))
            {
                entries.Add(entry);
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_NoOp_DoesNotThrow()
    {
        var reader = new MockSerialReader(MockMode.Deterministic);

        var act = async () => await reader.DisposeAsync();

        await act.Should().NotThrowAsync();
    }
```

- [ ] **Step 2: Run tests to verify the new ones fail**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockSerialReaderTests"
```

Expected: `ReadAsync_RandomMode_RespectsDelayBetweenEntries` and `ReadAsync_CancellationRequested_StopsEmitting` FAIL (random mode yields nothing). The other new tests (pre-cancelled, dispose) may already pass.

- [ ] **Step 3: Implement random mode**

Replace the `else` branch in `ReadAsync` in `src/Infrastructure/MockSerialReader.cs`:

```csharp
        else
        {
            var hexTemplates = new[]
            {
                "0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2} 0x{4:X2} 0x{5:X2}",
                "0x{0:X2} 0x{1:X2} 0x{2:X2} 0x{3:X2}",
            };

            var errnoTemplates = new[]
            {
                ("ERROR: ENOMEM - Cannot allocate memory (requested {0} bytes)", SerialLogLevel.Error),
                ("ERROR: EACCES - Permission denied for resource {0}", SerialLogLevel.Error),
                ("ERROR: ETIMEDOUT - Connection timed out after {0}ms", SerialLogLevel.Error),
                ("FATAL: SIGSEGV - Segmentation fault at 0x{0:X8}", SerialLogLevel.Fatal),
                ("FATAL: SIGBUS - Bus error at 0x{0:X8}", SerialLogLevel.Fatal),
            };

            var stackTemplates = new[]
            {
                "#0 0x{0:X8} in {1}() at {2}:{3}",
                "#1 0x{0:X8} in {1}() at {2}:{3}",
            };

            var functionNames = new[] { "main", "init_hardware", "read_sensor", "write_flash", "reset_handler" };
            var fileNames = new[] { "firmware.c", "hal.c", "sensor.c", "flash.c", "startup.s" };

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var category = _random.Next(3);
                string rawLine;
                SerialLogLevel level;

                switch (category)
                {
                    case 0: // Hex dump
                    {
                        var template = hexTemplates[_random.Next(hexTemplates.Length)];
                        var byteCount = template.Count(c => c == '{');
                        var args = Enumerable.Range(0, byteCount)
                            .Select(_ => (object)_random.Next(256)).ToArray();
                        rawLine = string.Format(template, args);
                        level = SerialLogLevel.Debug;
                        break;
                    }
                    case 1: // Errno
                    {
                        var (template, errLevel) = errnoTemplates[_random.Next(errnoTemplates.Length)];
                        rawLine = string.Format(template, _random.Next(1, 65536));
                        level = errLevel;
                        break;
                    }
                    default: // Stack trace
                    {
                        var template = stackTemplates[_random.Next(stackTemplates.Length)];
                        rawLine = string.Format(
                            template,
                            _random.Next(0x08000000, 0x08FFFFFF),
                            functionNames[_random.Next(functionNames.Length)],
                            fileNames[_random.Next(fileNames.Length)],
                            _random.Next(1, 500));
                        level = SerialLogLevel.Error;
                        break;
                    }
                }

                yield return new LogEntry(_timeProvider.GetUtcNow(), rawLine, level);

                if (_delay is { } d)
                {
                    await Task.Delay(d, cancellationToken);
                }
            }
        }
```

- [ ] **Step 4: Run all MockSerialReader tests**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockSerialReaderTests"
```

Expected: All 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/SerialSavant.Tests/MockSerialReaderTests.cs src/Infrastructure/MockSerialReader.cs
git commit -m "feat(infra): implement MockSerialReader random mode with cancellation tests"
```

---

### Task 5: MockLlmAnalyzer — content-based heuristic (TDD)

**Files:**
- Create: `tests/SerialSavant.Tests/MockLlmAnalyzerTests.cs`
- Create: `src/Infrastructure/MockLlmAnalyzer.cs`

- [ ] **Step 1: Write all failing tests**

Create `tests/SerialSavant.Tests/MockLlmAnalyzerTests.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using AwesomeAssertions;
using SerialSavant.Core;
using SerialSavant.Infrastructure;

namespace SerialSavant.Tests;

public sealed class MockLlmAnalyzerTests
{
    private readonly MockLlmAnalyzer _analyzer = new();

    private static LogEntry MakeEntry(string rawLine) =>
        new(DateTimeOffset.UtcNow, rawLine, SerialLogLevel.Unknown);

    [Theory]
    [InlineData("0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD")]
    [InlineData("0xCA 0xFE 0xBA 0xBE 0x00 0x00 0x01")]
    [InlineData("0x7F 0x45 0x4C 0x46 0x02 0x01 0x01")]
    public async Task AnalyzeAsync_HexDumpInput_ReturnsLowSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.Low);
    }

    [Theory]
    [InlineData("ERROR: ENOMEM - Cannot allocate memory", Severity.High)]
    [InlineData("ERROR: EACCES - Permission denied", Severity.High)]
    [InlineData("FATAL: SIGSEGV - Segmentation fault at 0x00000010", Severity.Critical)]
    [InlineData("FATAL: SIGBUS - Bus error at 0x0000DEAD", Severity.Critical)]
    public async Task AnalyzeAsync_ErrnoInput_ReturnsHighOrCriticalSeverity(
        string rawLine, Severity expectedSeverity)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(expectedSeverity);
    }

    [Theory]
    [InlineData("#0 0x0800ABCD in main() at firmware.c:142")]
    [InlineData("#1 0x0800EF01 in init_hardware() at hal.c:87")]
    public async Task AnalyzeAsync_StackTraceInput_ReturnsHighSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.High);
    }

    [Theory]
    [InlineData("Some random log message")]
    [InlineData("INFO: System started")]
    [InlineData("")]
    public async Task AnalyzeAsync_UnknownInput_ReturnsMediumSeverity(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Severity.Should().Be(Severity.Medium);
    }

    [Fact]
    public async Task AnalyzeAsync_SameInput_ReturnsDeterministicResult()
    {
        var entry = MakeEntry("ERROR: ENOMEM - Cannot allocate memory");

        var result1 = await _analyzer.AnalyzeAsync(entry);
        var result2 = await _analyzer.AnalyzeAsync(entry);

        result1.Explanation.Should().Be(result2.Explanation);
        result1.Severity.Should().Be(result2.Severity);
        result1.Suggestions.Should().BeEquivalentTo(result2.Suggestions);
    }

    [Theory]
    [InlineData("0x00 0x1A 0x2F")]
    [InlineData("ERROR: ENOMEM - Cannot allocate memory")]
    [InlineData("#0 0x0800ABCD in main() at firmware.c:142")]
    [InlineData("Some random log message")]
    public async Task AnalyzeAsync_ReturnsNonEmptyExplanationAndSuggestions(string rawLine)
    {
        var result = await _analyzer.AnalyzeAsync(MakeEntry(rawLine));

        result.Explanation.Should().NotBeNullOrWhiteSpace();
        result.Suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var entry = MakeEntry("test");

        var act = () => _analyzer.AnalyzeAsync(entry, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
```

- [ ] **Step 2: Create minimal MockLlmAnalyzer skeleton so tests compile**

Create `src/Infrastructure/MockLlmAnalyzer.cs`:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed class MockLlmAnalyzer : ILlmAnalyzer
{
    public Task<AnalysisResult> AnalyzeAsync(
        LogEntry entry, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockLlmAnalyzerTests"
```

Expected: All 7 test cases FAIL with `NotImplementedException`.

- [ ] **Step 4: Implement the content-based heuristic**

Replace `src/Infrastructure/MockLlmAnalyzer.cs` with the full implementation:

```csharp
// SPDX-FileCopyrightText: 2026 Oliver Raider
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using SerialSavant.Core;

namespace SerialSavant.Infrastructure;

public sealed partial class MockLlmAnalyzer : ILlmAnalyzer
{
    private static readonly string[] CriticalSignals = ["SIGSEGV", "SIGBUS"];

    private static readonly string[] ErrnoNames =
        ["ENOMEM", "EACCES", "ETIMEDOUT", "EINVAL", "ENOENT", "SIGSEGV", "SIGBUS", "SIGABRT"];

    public Task<AnalysisResult> AnalyzeAsync(
        LogEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rawLine = entry.RawLine;
        var result = Classify(rawLine);

        return Task.FromResult(result);
    }

    private static AnalysisResult Classify(string rawLine)
    {
        if (ContainsErrno(rawLine, out var errnoName))
        {
            var isCritical = CriticalSignals.Any(s =>
                errnoName.Contains(s, StringComparison.Ordinal));

            return new AnalysisResult(
                Explanation: $"POSIX error detected: {errnoName}. This indicates a system-level failure that may require immediate attention.",
                Severity: isCritical ? Severity.Critical : Severity.High,
                Suggestions: isCritical
                    ? ["Inspect pointer validity", "Check for stack overflow", "Review memory map"]
                    : ["Check memory allocation", "Verify resource permissions", "Review system limits"]);
        }

        if (StackTracePattern().IsMatch(rawLine))
        {
            return new AnalysisResult(
                Explanation: "Stack trace frame detected. This indicates a crash or exception in the firmware execution path.",
                Severity: Severity.High,
                Suggestions: ["Check stack depth", "Review function at crash point", "Inspect call chain for recursion"]);
        }

        if (HexDumpPattern().IsMatch(rawLine))
        {
            return new AnalysisResult(
                Explanation: "Hex dump detected. Raw byte data from device memory or communication buffer.",
                Severity: Severity.Low,
                Suggestions: ["Check byte alignment", "Verify endianness"]);
        }

        return new AnalysisResult(
            Explanation: "Unrecognized log entry. Unable to determine specific pattern.",
            Severity: Severity.Medium,
            Suggestions: ["Review log context"]);
    }

    private static bool ContainsErrno(string rawLine, out string errnoName)
    {
        foreach (var name in ErrnoNames)
        {
            if (rawLine.Contains(name, StringComparison.Ordinal))
            {
                errnoName = name;
                return true;
            }
        }

        errnoName = string.Empty;
        return false;
    }

    [GeneratedRegex(@"#\d+\s+0x[0-9A-Fa-f]+\s+in\s+")]
    private static partial Regex StackTracePattern();

    [GeneratedRegex(@"0x[0-9A-Fa-f]{2}(\s+0x[0-9A-Fa-f]{2})+")]
    private static partial Regex HexDumpPattern();
}
```

Key design points:
- Errno check runs **before** hex check because errno lines like `"FATAL: SIGSEGV - Segmentation fault at 0x00000010"` also contain hex patterns. Errno takes priority.
- `[GeneratedRegex]` is AOT-compatible (source-generated, no reflection).
- `partial class` + `partial Regex` methods are required for the source generator.

- [ ] **Step 5: Run all MockLlmAnalyzer tests**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~MockLlmAnalyzerTests"
```

Expected: All 7 test cases PASS (the `[Theory]` tests expand to multiple cases, but all grouped under 7 test methods).

- [ ] **Step 6: Run the full test suite**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet test --logger "console;verbosity=detailed"
```

Expected: All 14 tests PASS (7 MockSerialReader + 7 MockLlmAnalyzer).

- [ ] **Step 7: Commit**

```bash
git add tests/SerialSavant.Tests/MockLlmAnalyzerTests.cs src/Infrastructure/MockLlmAnalyzer.cs
git commit -m "feat(infra): implement MockLlmAnalyzer with content-based heuristic

Closes #1"
```

---

### Task 6: Final verification and cleanup

**Files:**
- No new files — verification only

- [ ] **Step 1: Full build and test**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet build
dotnet test --logger "console;verbosity=detailed"
```

Expected: Build clean, all tests green.

- [ ] **Step 2: Verify AOT compatibility**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet restore
dotnet publish SerialSavant.csproj -c Release 2>&1 | grep -i "warning IL"
```

Expected: No IL2026/IL3050 warnings from Infrastructure project files.

- [ ] **Step 3: Run dotnet format**

```bash
cd /home/oliverraider/Projects/Open-Source/SerialSavant
dotnet format --verify-no-changes
```

If the check fails (formatting needed), run `dotnet format` and commit:

```bash
dotnet format
git add -u
git commit -m "style: apply dotnet format"
```
