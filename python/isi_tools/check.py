#!/usr/bin/env python3
"""ISI IDE bridge: syntax check + optional linters.

Usage:  python check.py <file.py>
Prints a JSON array of diagnostics to stdout:
  [{"severity": "error|warning", "message": str, "line": int, "column": int, "source": str}]

Uses only the standard library. If `ruff` (preferred) or `pyflakes` is installed
in the selected interpreter, their findings are added after a clean syntax check.
"""
from __future__ import annotations

import ast
import importlib.util
import json
import re
import subprocess
import sys


def syntax_diagnostics(path: str, source: str) -> list[dict]:
    try:
        ast.parse(source, filename=path)
    except SyntaxError as exc:
        return [{
            "severity": "error",
            "message": exc.msg or "Syntax error",
            "line": exc.lineno or 1,
            "column": exc.offset or 1,
            "source": "python",
        }]
    return []


def _has_module(name: str) -> bool:
    return importlib.util.find_spec(name) is not None


def ruff_diagnostics(path: str) -> list[dict]:
    proc = subprocess.run(
        [sys.executable, "-m", "ruff", "check", "--output-format", "json", "--no-cache", path],
        capture_output=True, text=True, timeout=60,
    )
    try:
        items = json.loads(proc.stdout or "[]")
    except json.JSONDecodeError:
        return []
    result = []
    for item in items:
        loc = item.get("location") or {}
        code = item.get("code") or "ruff"
        result.append({
            "severity": "warning",
            "message": f"{code}: {item.get('message', '')}",
            "line": loc.get("row", 1),
            "column": loc.get("column", 1),
            "source": "ruff",
        })
    return result


_PYFLAKES_LINE = re.compile(r"^.*?:(\d+):(?:(\d+):?)?\s*(.*)$")


def pyflakes_diagnostics(path: str) -> list[dict]:
    proc = subprocess.run(
        [sys.executable, "-m", "pyflakes", path],
        capture_output=True, text=True, timeout=60,
    )
    result = []
    for line in proc.stdout.splitlines():
        m = _PYFLAKES_LINE.match(line)
        if m:
            result.append({
                "severity": "warning",
                "message": m.group(3),
                "line": int(m.group(1)),
                "column": int(m.group(2) or 1),
                "source": "pyflakes",
            })
    return result


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print("usage: check.py <file.py>", file=sys.stderr)
        return 2
    path = argv[1]
    with open(path, encoding="utf-8", errors="replace") as fh:
        source = fh.read()

    diagnostics = syntax_diagnostics(path, source)
    if not diagnostics:
        try:
            if _has_module("ruff"):
                diagnostics = ruff_diagnostics(path)
            elif _has_module("pyflakes"):
                diagnostics = pyflakes_diagnostics(path)
        except (OSError, subprocess.SubprocessError):
            pass  # linters are optional; never fail the whole check
    sys.stdout.write(json.dumps(diagnostics, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
