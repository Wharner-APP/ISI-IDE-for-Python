# Contributing

ISI IDE for Python is proprietary software of Wharner Group; contributions are accepted from
Wharner APP team members and contributors who have signed a contributor agreement.

## Code style
- **C++** - Google C++ Style Guide, C++20, no exceptions across the C ABI (`core/src/core.cpp`).
- **C#** - Microsoft C# conventions, nullable reference types enabled, MVVM: no UI types in view-models
  except `TextDocument` and `Dispatcher`; use `IDialogService` for dialogs.
- **Python** - PEP 8, standard library only for `python/isi_tools` (it runs inside the *user's* interpreter).

## Workflow
1. Branch from `main`: `feature/<short-name>`.
2. Build locally with `build.ps1` / `build.sh`; native tests run with `ctest --test-dir build-native`.
3. Add an entry to `CHANGELOG.md`.
4. Open a pull request; CI must build all five targets.

## Adding a native function
1. Declare it in `core/include/isi/core.h` (plain C types only) and bump `ISI_CORE_ABI_VERSION` if breaking.
2. Implement it in `core/src`, add a test in `core/tests`.
3. Add the `DllImport` in `src/ISI.IDE/Services/NativeCore.cs` with a managed fallback.
