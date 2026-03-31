# SerialSavant

**Local-AI powered serial log analyzer for embedded developers.**

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-Apache--2.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)
![.NET](https://img.shields.io/badge/.NET-10-purple)

---

## Why SerialSavant?

- **You are reading raw UART logs at 3 AM.** Hex dumps, hard fault addresses, and errno codes are not self-explanatory — SerialSavant explains them inline, as they arrive.
- **Cloud-based AI tools are not an option for embedded work.** Firmware logs contain proprietary protocol data. SerialSavant runs everything 100% locally — no network calls, no telemetry, no data ever leaves your machine.
- **Context switching kills debugging flow.** Instead of alt-tabbing to a browser to decode a stack frame, the explanation appears directly in your terminal alongside the raw line.
- **Hardware is not always available.** Mock implementations let you develop and test the analysis pipeline on any machine without a physical serial adapter.

---

## Features

- Real-time serial UART log capture via configurable COM port / baud rate
- Local LLM analysis via [llama-server](https://github.com/ggml-org/llama.cpp) sidecar (OpenAI-compatible HTTP API)
- Explains hex dumps, stack traces, and POSIX error codes automatically
- Native AOT binary — single file, fast startup, no .NET runtime required on the target machine
- Automatic reconnect with exponential backoff on connection loss
- Interactive setup wizard for first-run configuration
- Spectre.Console live table: `timestamp | log level | raw line | AI explanation | severity`
- Export session to JSON or plain text
- Graceful Ctrl+C shutdown
- Cross-platform: Windows, Linux, macOS
- Mock mode for development without hardware

---

## Requirements

### To run a pre-built binary

- The `SerialSavant` binary (from [Releases](https://github.com/twixinator/SerialSavant/releases))
- `llama-server` binary — see [Getting llama-server](#getting-llama-server)
- A GGUF model file — see [Getting a GGUF Model](#getting-a-gguf-model)
- A serial adapter / USB-to-UART device (or use mock mode to test without hardware)

### To build from source

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `llama-server` binary
- A GGUF model file

---

## Quick Start

No hardware? No problem. Use mock mode to verify the full pipeline on any machine.

```bash
git clone https://github.com/twixinator/SerialSavant.git
cd SerialSavant
dotnet run --project src/UI -- --mock
```

The interactive setup wizard will run on first launch. Mock mode replaces the serial reader and LLM analyzer with deterministic fakes so you can observe the UI and export pipeline without a device or model.

---

## Getting llama-server

`llama-server` is part of [llama.cpp](https://github.com/ggml-org/llama.cpp).

1. Go to the [llama.cpp releases page](https://github.com/ggml-org/llama.cpp/releases/latest).
2. Download the archive for your platform (e.g., `llama-<version>-bin-win-avx2-x64.zip` on Windows, `llama-<version>-bin-ubuntu-x64.zip` on Linux).
3. Extract and place the `llama-server` (or `llama-server.exe`) binary somewhere on your `PATH`, or point to it directly in `config.json`.

SerialSavant spawns `llama-server` as a subprocess and communicates with it over `localhost`. The process is shut down cleanly on exit.

---

## Getting a GGUF Model

Any GGUF-format model works. For embedded log analysis, prefer small, fast models:

| Model | Size | Notes |
|---|---|---|
| Phi-3.5-mini-instruct | ~2.2 GB (Q4) | Good reasoning, fast on CPU |
| Qwen2.5-3B-Instruct | ~1.9 GB (Q4) | Compact and capable |
| Llama-3.2-3B-Instruct | ~2.0 GB (Q4) | Solid general purpose |

Download from [Hugging Face](https://huggingface.co/models?library=gguf&sort=downloads). Search for the model name and look for a GGUF repository (often maintained by `bartowski` or `unsloth`).

Place the `.gguf` file anywhere accessible and set the path in `config.json` or enter it during the setup wizard.

---

## Build from Source

```bash
git clone https://github.com/twixinator/SerialSavant.git
cd SerialSavant
dotnet build
dotnet test
```

---

## AOT Build (Release Binaries)

SerialSavant supports Native AOT compilation. The output is a single self-contained binary with no .NET runtime dependency.

```bash
# Windows
dotnet publish -r win-x64 -c Release

# Linux
dotnet publish -r linux-x64 -c Release

# macOS (Apple Silicon)
dotnet publish -r osx-arm64 -c Release

# macOS (Intel)
dotnet publish -r osx-x64 -c Release
```

Output lands in `bin/Release/net10.0/<rid>/publish/`.

> **Note:** AOT compilation requires the target platform's native toolchain (MSVC on Windows, GCC/Clang on Linux/macOS) to be present on the build machine.

---

## Configuration

On first run, the setup wizard writes `~/.serialsavant/config.json`. You can also edit it directly.

```jsonc
{
  "serial": {
    "portName": "COM3",          // e.g. /dev/ttyUSB0 on Linux
    "baudRate": 115200,
    "reconnect": {
      "enabled": true,
      "maxAttempts": 10,
      "initialDelayMs": 500      // doubles on each retry (exponential backoff)
    }
  },
  "llm": {
    "llamaServerPath": "llama-server",   // path to binary or name if on PATH
    "modelPath": "/models/phi-3.5-mini.Q4_K_M.gguf",
    "contextSize": 2048,
    "port": 8080
  },
  "app": {
    "exportPath": "~/serialsavant-sessions",
    "exportFormat": "json"       // "json" or "text"
  }
}
```

---

## Architecture Overview

| Project | Role |
|---|---|
| `SerialSavant.Core` | Interfaces (`ISerialReader`, `ILlmAnalyzer`) and domain models (`LogEntry`, `AnalysisResult`) |
| `SerialSavant.Config` | Configuration types: `SerialConfig` (with reconnect policy), `LlmConfig`, `AppSettings` |
| `SerialSavant.Infrastructure` | `UartSerialReader` (exponential backoff reconnect), `LlamaCppAnalyzer` (HTTP → llama-server) |
| `SerialSavant.UI` | `ConsoleOrchestrator` — Spectre.Console live table, setup wizard, session export |

The UI layer depends only on Core interfaces. Infrastructure implementations are registered at startup, making it straightforward to swap in mock implementations or alternative backends.

---

## Contributing

Contributions are welcome. Before submitting a pull request, please review [`CLA.md`](CLA.md) — a Contributor License Agreement signature is required.

- Open bugs and feature requests on the [issue tracker](https://github.com/twixinator/SerialSavant/issues).
- For non-trivial changes, open an issue first to discuss the approach.
- Run `dotnet test` before submitting. All tests must pass.

---

## License

Apache-2.0. See [`LICENSE`](LICENSE) for the full text.

Copyright 2026 Oliver Raider
