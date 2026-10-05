# Roadmap

## v0.1 (this release) - MVP
Editor, project tree, run/stop, interpreter selection, pip GUI, basic Git, native scanner, Python bridge, CI for 5 targets.

## v0.2 - Productivity
- Embedded terminal (PTY: ConPTY on Windows, forkpty on Unix) with PowerShell/bash/zsh.
- Plugin host (Python API, see PLUGIN_API.md) + plugin manager UI.
- Find/replace in project (regex, masks), TODO manager, bookmarks, split view.
- Venv/poetry/pipenv/conda detection + project wizard with templates (Django, Flask, FastAPI, CLI, library).
- Formatters (black/ruff format/autopep8) and ruff/mypy diagnostics inline in the editor.
- Localization (ru/en), onboarding wizard, local file history, autosave.

## v0.3 - Code intelligence
- Real C++ parser + AST + symbol index; go to definition, find usages, class hierarchy.
- Semantic completion with type hints, auto-import, optimize imports.
- Refactorings: rename, extract method/variable, inline, move.

## v0.4 - Debugging & testing
- Debugger via DAP/debugpy: breakpoints (conditional, logpoints), step in/out/over, variables, watches, call stack, evaluate.
- Run configurations; pytest/unittest runner; coverage.py visualization; cProfile / line_profiler.

## v0.5 - Git & integrations
- Full Git client: history graph, branches, merge/rebase/cherry-pick/stash, three-pane conflict resolver, blame, diff.
- GitHub OAuth, pull requests, issues, Actions; GitLab/Bitbucket.
- Docker/docker-compose, SSH/SFTP, database browser, Jupyter notebooks.

## 1.0 - Release
Stability, performance budget (start < 3 s, UI latency < 100 ms), signed installers (MSI/MSIX, deb/rpm/AppImage, notarized macOS app), auto-update, opt-in telemetry.

## 2.0
Plugin marketplace, settings sync, remote development, collaborative editing, Vim/Emacs modes, AI assistant integration.
