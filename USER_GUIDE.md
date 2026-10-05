# ISI IDE for Python - User guide (v0.1)

## First steps
1. Start the IDE. A small welcome file opens - press **F5** to run it. Type your name in the **Run** panel input
   field and press Enter.
2. **File → Open folder…** to open a project. Double-click files in the tree to edit them.
3. **Tools → Settings** to change theme, font size and the Python interpreter.

## Shortcuts
| Action | Keys |
|---|---|
| New / Open / Open folder | Ctrl+N / Ctrl+O / Ctrl+Shift+O |
| Save / Save as | Ctrl+S / Ctrl+Shift+S |
| Run | F5 or Shift+F10 |
| Stop | Ctrl+F2 |
| Check file | F7 |
| Find in file | Ctrl+F |
| Settings | Ctrl+Alt+S |

## Python interpreter
Default: `Python 3.14\bin\python.exe` inside the IDE folder (`python3` on Linux/macOS). Change it in
Settings; **Default** restores the bundled path. If nothing is found, the IDE tries `PATH`.

## Packages
**Tools → Packages (pip)** lists packages of the selected interpreter; type a requirement (e.g. `requests`
or `numpy>=2.0`) and click **Install**.

## Git
Open a project folder that is a git repository. **Git** menu: status, commit (stages everything), pull, push,
log. Output appears in the **Run** panel; the branch name is shown in the status bar. Git must be installed.

## Checking code
**Check** (F7) runs the Python bridge: syntax errors via `ast`, plus **ruff** or **pyflakes** if installed
in the selected interpreter (`pip install ruff`). Double-click a problem to jump to the line.
