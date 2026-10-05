# Plugin API (specification for v0.2 - not loaded by v0.1)

Plugins are folders under `%APPDATA%/Wharner APP/ISI IDE for Python/plugins/<id>/` (or `~/.config/...`).

## plugin.json
```json
{
  "id": "hello-plugin",
  "name": "Hello plugin",
  "version": "0.1.0",
  "author": "Wharner APP",
  "entry": "main.py",
  "isi_api": "1"
}
```

## main.py
```python
from isi_api import ide, command

@command("hello.say", title="Say hello", shortcut="Ctrl+Alt+H")
def say_hello():
    ide.status("Hello from a plugin!")
    editor = ide.active_editor()
    if editor:
        editor.insert_at_caret("print('Hello')\n")

def activate():      # called once on start-up
    ide.log("hello-plugin activated")

def deactivate():    # called on shutdown / disable
    pass
```

## Planned `isi_api` surface
`ide.status(text)`, `ide.log(text)`, `ide.active_editor()` (`.text`, `.path`, `.insert_at_caret`, `.replace_selection`),
`ide.open_file(path)`, `ide.project_root()`, `ide.run_process(args)`, events: `on_save`, `on_open`, `on_run`.

Plugins run in a **separate Python process** talking JSON-RPC over stdio with the IDE, so a faulty plugin cannot
crash the editor. A C# extension interface (`IIdePlugin`) will be offered for in-process plugins.
