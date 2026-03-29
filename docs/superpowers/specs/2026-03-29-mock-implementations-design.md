# Mock Implementations & Unit Tests — Design Spec

**Issues:** #1 (mock implementations + tests), #6 (test project setup)
**Date:** 2026-03-29

## Overview

Add `MockSerialReader` and `MockLlmAnalyzer` to `src/Infrastructure/` for local development and testing without real hardware or LLM. Create the xUnit test project at `tests/SerialSavant.Tests/` with full TDD coverage.

## Components

### MockMode Enum

```csharp
public enum MockMode
{
    Deterministic,
    Random
}
```

Placed in `src/Infrastructure/MockMode.cs`.

### MockSerialReader

**Location:** `src/Infrastructure/MockSerialReader.cs`
**Implements:** `ISerialReader`

**Constructor parameters:**
- `MockMode mode` — selects deterministic or random behavior
- `TimeSpan? delay = null` — delay between emitted lines (null = no delay for tests, ~100ms for demo)
- `TimeProvider? timeProvider = null` — for testable timestamps (defaults to `TimeProvider.System`)

**Deterministic mode:**
Fixed sequence of ~10 `LogEntry` items covering all three categories:
- 3-4 hex dump lines (e.g., `"0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD"`)
- 3-4 C errno lines (e.g., `"ERROR: ENOMEM - Cannot allocate memory"`, `"FATAL: SIGSEGV - Segmentation fault at 0x00000010"`)
- 2-3 stack trace lines (e.g., `"#0 0x0800ABCD in main() at firmware.c:142"`)

Each entry has an appropriate `SerialLogLevel` assigned. The sequence is yielded once and completes.

**Random mode:**
Infinite stream. Each iteration randomly selects a category (hex/errno/stack) and generates a line with varied content from predefined templates. Respects the configured delay between entries. Never completes — runs until cancellation.

**Both modes:**
- Respect `CancellationToken` at every yield point
- `DisposeAsync()` is a no-op (no real resources held)

### MockLlmAnalyzer

**Location:** `src/Infrastructure/MockLlmAnalyzer.cs`
**Implements:** `ILlmAnalyzer`

**Content-based heuristic detection on `LogEntry.RawLine`:**

| Pattern detected | Severity | Explanation style | Suggestions |
|---|---|---|---|
| Hex bytes (`0x`, sequences of `[0-9A-Fa-f]{2}`) | Low | Byte interpretation, common patterns (magic bytes, null terminators) | "Check byte alignment", "Verify endianness" |
| C errno names (`ENOMEM`, `SIGSEGV`, `EACCES`, etc.) | High or Critical (`SIGSEGV`/`SIGBUS` = Critical) | POSIX errno explanation | "Check memory allocation", "Inspect pointer validity" |
| Stack traces (`#0`, `at 0x`, `in ... at ...`) | High | Frame analysis, crash pattern description | "Check stack depth", "Review function at crash point" |
| None of the above | Medium | Generic "unrecognized log entry" | "Review log context" |

**Properties:**
- Deterministic: same `RawLine` always produces the same `AnalysisResult`
- Respects `CancellationToken` (throws `OperationCanceledException` if already cancelled)
- No async I/O — returns `Task.FromResult` (but signature stays async for interface compatibility)

### Test Project

**Location:** `tests/SerialSavant.Tests/`

**Setup:**
- `dotnet new xunit` at `tests/SerialSavant.Tests`
- Added to `SerialSavant.sln`
- References: `SerialSavant.Core`, `SerialSavant.Infrastructure`
- NuGet packages: `xunit`, `xunit.runner.visualstudio`, `AwesomeAssertions`, `NSubstitute`
- `IsAotCompatible=false`

**Test naming convention:** `[Class]Tests.[Method]_[Scenario]_[Expected]`

### Test Cases

#### MockSerialReaderTests

| Test | Description |
|---|---|
| `ReadAsync_DeterministicMode_ReturnsExpectedSequence` | Deterministic mode yields the exact fixed sequence of entries |
| `ReadAsync_DeterministicMode_AllLogCategoriesPresent` | Output contains at least one hex dump, one errno, and one stack trace |
| `ReadAsync_RandomMode_RespectsDelayBetweenEntries` | With delay configured, elapsed time between entries is >= delay |
| `ReadAsync_CancellationRequested_StopsEmitting` | Cancelling the token stops the enumeration cleanly |

#### MockLlmAnalyzerTests

| Test | Description |
|---|---|
| `AnalyzeAsync_HexDumpInput_ReturnsLowSeverity` | Hex dump line produces `Severity.Low` |
| `AnalyzeAsync_ErrnoInput_ReturnsHighOrCriticalSeverity` | Errno line produces `Severity.High` or `Severity.Critical` |
| `AnalyzeAsync_StackTraceInput_ReturnsHighSeverity` | Stack trace line produces `Severity.High` |
| `AnalyzeAsync_UnknownInput_ReturnsMediumSeverity` | Unrecognized input produces `Severity.Medium` |
| `AnalyzeAsync_SameInput_ReturnsDeterministicResult` | Same input twice produces identical output |
| `AnalyzeAsync_CancellationRequested_ThrowsOperationCanceled` | Pre-cancelled token throws `OperationCanceledException` |

## Dependencies

- `TimeProvider` (BCL, .NET 8+) for testable timestamps in MockSerialReader
- No external dependencies beyond existing project references and test packages

## AOT Compatibility

- Both mocks must remain AOT-compatible (`IsAotCompatible=true` on Infrastructure project)
- No reflection, no `Assembly.GetTypes()`, no dynamic code generation
- Test project is exempt (`IsAotCompatible=false`)
