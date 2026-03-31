# Mock Implementations & Unit Tests — Design Spec

**Issues:** #1 (mock implementations + tests), #6 (test project setup)
**Date:** 2026-03-29

## Overview

Add `MockSerialReader` and `MockLlmAnalyzer` to `src/Infrastructure/` for local development and testing without real hardware or LLM. Create the xUnit test project at `tests/SerialSavant.Tests/` with full TDD coverage.

All new `.cs` files carry the project SPDX header (`SPDX-FileCopyrightText: 2026 Oliver Raider`, `SPDX-License-Identifier: Apache-2.0`).

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

Note: If mock mode selection is ever config-driven, `MockMode` may need to migrate to `Config`.

### MockSerialReader

**Location:** `src/Infrastructure/MockSerialReader.cs`
**Implements:** `ISerialReader`

**Constructor parameters:**
- `MockMode mode` — selects deterministic or random behavior
- `TimeSpan? delay = null` — delay between emitted lines (null = no delay for tests, ~100ms for demo)
- `TimeProvider? timeProvider = null` — for testable timestamps (defaults to `TimeProvider.System`)
- `Random? random = null` — for deterministic random output in tests (defaults to `new Random()`)

Timestamps are obtained via `timeProvider.GetUtcNow()`.

**Deterministic mode:**
Fixed sequence of ~10 `LogEntry` items covering all three categories:
- 3-4 hex dump lines (e.g., `"0x00 0x1A 0x2F 0x00 0xFF 0xDE 0xAD"`)
- 3-4 C errno lines (e.g., `"ERROR: ENOMEM - Cannot allocate memory"`, `"FATAL: SIGSEGV - Segmentation fault at 0x00000010"`)
- 2-3 stack trace lines (e.g., `"#0 0x0800ABCD in main() at firmware.c:142"`)

Each entry has an appropriate `SerialLogLevel` assigned. The sequence is yielded once and completes (finite stream).

**Random mode:**
Infinite stream. Each iteration uses the injected `Random` instance to select a category (hex/errno/stack) and generate a line with varied content from predefined templates. Tests pass `new Random(fixedSeed)` for reproducible output. Respects the configured delay between entries. Never completes — runs until cancellation.

**Both modes:**
- Respect `CancellationToken` at every yield point (including entry — pre-cancelled token stops immediately)
- `DisposeAsync()` is a no-op (no real resources held)

### MockLlmAnalyzer

**Location:** `src/Infrastructure/MockLlmAnalyzer.cs`
**Implements:** `ILlmAnalyzer`

**Content-based heuristic detection on `LogEntry.RawLine`:**

The mock inspects only `RawLine`; `SerialLogLevel` is ignored. The mock never returns `Severity.Unknown`.

| Pattern detected | Severity | Explanation style | Suggestions |
|---|---|---|---|
| Hex bytes (`0x`, sequences of `[0-9A-Fa-f]{2}`) | Low | Byte interpretation, common patterns (magic bytes, null terminators) | "Check byte alignment", "Verify endianness" |
| C errno names (`ENOMEM`, `SIGSEGV`, `EACCES`, etc.) | High or Critical (`SIGSEGV`/`SIGBUS` = Critical) | POSIX errno explanation | "Check memory allocation", "Inspect pointer validity" |
| Stack traces (`#0`, `at 0x`, `in ... at ...`) | High | Frame analysis, crash pattern description | "Check stack depth", "Review function at crash point" |
| None of the above | Medium | Generic "unrecognized log entry" | "Review log context" |

**Properties:**
- Deterministic: same `RawLine` always produces the same `AnalysisResult`
- `Explanation` is always non-empty; `Suggestions` always contains at least one entry
- Respects `CancellationToken` (throws `OperationCanceledException` if already cancelled)
- No async I/O — returns `Task.FromResult` (but signature stays async for interface compatibility)

### Test Project

**Location:** `tests/SerialSavant.Tests/`

**Setup:**
- `dotnet new xunit --framework net10.0` at `tests/SerialSavant.Tests`
- Added to `SerialSavant.sln`
- References: `SerialSavant.Core`, `SerialSavant.Infrastructure`
- NuGet packages: `xunit`, `xunit.runner.visualstudio`, `AwesomeAssertions`
- `IsAotCompatible=false`

NSubstitute is not needed for these tests (both mocks are concrete subjects with no injected collaborators beyond `TimeProvider` and `Random`). It will be added when tests for real implementations require interface substitution.

**Test naming convention:** `[Class]Tests.[Method]_[Scenario]_[Expected]`

### Test Cases

#### MockSerialReaderTests

| Test | Description |
|---|---|
| `ReadAsync_DeterministicMode_ReturnsExpectedSequence` | Deterministic mode yields the exact fixed sequence of entries |
| `ReadAsync_DeterministicMode_AllLogCategoriesPresent` | Output contains at least one hex dump, one errno, and one stack trace |
| `ReadAsync_DeterministicMode_CompletesAfterFullSequence` | Enumeration terminates after the fixed sequence (does not hang) |
| `ReadAsync_RandomMode_RespectsDelayBetweenEntries` | With delay configured, elapsed time between entries is >= delay |
| `ReadAsync_CancellationRequested_StopsEmitting` | Cancelling the token mid-stream stops the enumeration cleanly |
| `ReadAsync_PreCancelledToken_StopsImmediately` | Already-cancelled token causes immediate exit without yielding |
| `DisposeAsync_NoOp_DoesNotThrow` | DisposeAsync completes without error |

#### MockLlmAnalyzerTests

| Test | Description |
|---|---|
| `AnalyzeAsync_HexDumpInput_ReturnsLowSeverity` | Hex dump line produces `Severity.Low` |
| `AnalyzeAsync_ErrnoInput_ReturnsHighOrCriticalSeverity` | Errno line produces `Severity.High` or `Severity.Critical` |
| `AnalyzeAsync_StackTraceInput_ReturnsHighSeverity` | Stack trace line produces `Severity.High` |
| `AnalyzeAsync_UnknownInput_ReturnsMediumSeverity` | Unrecognized input produces `Severity.Medium` |
| `AnalyzeAsync_SameInput_ReturnsDeterministicResult` | Same input twice produces identical output |
| `AnalyzeAsync_ReturnsNonEmptyExplanationAndSuggestions` | Result has non-empty `Explanation` and at least one `Suggestion` |
| `AnalyzeAsync_CancellationRequested_ThrowsOperationCanceled` | Pre-cancelled token throws `OperationCanceledException` |

## Dependencies

- `TimeProvider` (BCL, .NET 8+) for testable timestamps in MockSerialReader
- `Random` (BCL) injectable for deterministic random-mode testing
- No external dependencies beyond existing project references and test packages

## AOT Compatibility

- Both mocks must remain AOT-compatible (`IsAotCompatible=true` on Infrastructure project)
- No reflection, no `Assembly.GetTypes()`, no dynamic code generation
- Test project is exempt (`IsAotCompatible=false`)
