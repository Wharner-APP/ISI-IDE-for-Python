"""Example plugin - specification only; the plugin host arrives in v0.2."""
from isi_api import ide, command  # noqa: F401  (provided by the IDE at runtime)


@command("hello.say", title="Say hello", shortcut="Ctrl+Alt+H")
def say_hello():
    ide.status("Hello from a plugin!")


def activate():
    ide.log("hello-plugin activated")
