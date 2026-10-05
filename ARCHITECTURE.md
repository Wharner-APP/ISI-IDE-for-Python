# Architecture

## Layers

```
┌──────────────────────────── ISI.IDE (C# / .NET 8 / Avalonia 11) ───────────────────────────┐
│ Views (AXAML)  ──binding──▶  ViewModels (CommunityToolkit.Mvvm)  ──▶  Services              │
│ MainWindow, EditorView        MainWindowVM, EditorTabVM, FileNodeVM     AppSettings          │
│ Settings/Packages/About       PackagesVM, SettingsVM, IDialogService    PythonLocator        │
│                                                                         ScriptRunner/Process │
│ Editor: PythonColorizer, PythonIndentationStrategy, CompletionProvider  GitService/PipService│
└───────────────────────────┬───────────────────────────────┬─────────────────────────────────┘
                            │ P/Invoke (C ABI, JSON)        │ child processes (stdin/stdout)
                  ┌─────────▼─────────┐             ┌───────▼──────────────────────────┐
                  │ isi_core (C++20)  │             │ Python interpreter (user-chosen) │
                  │ scanner/outline/  │             │  ├─ user script (Run)            │
                  │ diagnostics       │             │  ├─ isi_tools/check.py (bridge)  │
                  └───────────────────┘             │  └─ pip, ruff, pyflakes          │
                                                    └──────────────────────────────────┘
                                                    git CLI
```

## Why these choices

| Decision | Reason | Alternative |
|---|---|---|
| **Avalonia** instead of WPF | WPF is Windows-only; the product must run on Windows, Linux and macOS | MAUI (no Linux desktop) |
| **AvaloniaEdit** + own managed colorizer | Mature editor (folding, completion, search); a managed colorizer works on every RID, no native TextMate/Oniguruma dependency | TextMateSharp grammars (richer, but native libs per RID) |
| **P/Invoke + C ABI + JSON** for C++ ↔ C# | Zero-copy-free but simple, in-process (no IPC latency on each keystroke), trivially portable; the ABI is versioned | C++/CLI (Windows only), gRPC/local IPC (better isolation, planned for the heavy indexer in v0.3) |
| Python as **child processes**, not embedded CPython | A crash or hang in user code cannot take down the IDE; any interpreter/venv can be used; no ABI coupling to Python 3.14 | pybind11 embedding (planned only for in-process tooling) |
| Settings in the user profile | `Program Files` is not writable | — |

## Native ↔ managed contract

`isi_analyze_json(utf8) -> malloc'ed utf8 json`, released with `isi_free`. `isi_abi_version()` is checked at
start-up; if the library is missing, has a different ABI or throws, `CodeIntelligence` silently falls back to
a managed outline-only implementation, so the IDE never fails to start because of the native part.

## Threading

UI thread: documents, view-models. Worker threads: debounced analysis (400 ms after the last edit), process
output pumps. Everything that touches bindable state is marshalled with `Dispatcher.UIThread`.

## Platform matrix

| Target | .NET RID | Notes |
|---|---|---|
| Windows x64 / x86 | win-x64 / win-x86 | static MSVC runtime, no vcredist |
| Linux x64 | linux-x64 | built on Ubuntu 22.04 (glibc 2.35 baseline) |
| Linux 32-bit | linux-arm | **x86 32-bit Linux is not supported by .NET**; the slot is 32-bit ARM |
| macOS x64 | osx-x64 | cross-built on the arm64 runner; add `osx-arm64` to the matrix for Apple Silicon-native |

## Planned subsystems (not in v0.1)
Debugger (DAP via debugpy), full Git UI, refactoring engine, plugin host, Docker/DB/SSH tools,
Jupyter, embedded PTY terminal — see [docs/ROADMAP.md](docs/ROADMAP.md).
