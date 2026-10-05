# Changelog

All notable changes to **ISI IDE for Python** (© Wharner Group, developed by Wharner APP).

## [0.1.0] - first MVP

### Added
- Avalonia 11 / .NET 8 desktop application (MVVM), splash screen, About dialog.
- Editor: Python syntax highlighting, folding, auto-indent, simple completion, Ctrl+F search.
- Project tree, Structure panel (from the C++ core), Problems panel.
- Run/Stop of the current file with streamed output and console input.
- Configurable Python interpreter (default: `<IDE folder>/Python 3.14/bin/python.exe`).
- pip package manager window.
- Basic Git integration (status, commit, pull, push, log).
- C++20 native core (`isi_core`) exposing a C ABI, with unit tests.
- Python bridge (`isi_tools/check.py`): syntax check + ruff / pyflakes when installed.
- GitHub Actions workflow producing 5 archives (win-x64, win-x86, linux-x64, linux-arm32, macos-x64).
