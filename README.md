# ISI IDE for Python

**© Wharner Group · Developed by Wharner APP**
https://wharner-official-app.tilda.ws/isiidepython

Дружелюбная кроссплатформенная IDE для Python: что-то среднее между PyCharm и Thonny.
Чистый интерфейс для новичка и быстрые инструменты для профессионала.

> **Статус: v0.1.0 (MVP / первая версия).** Это рабочий фундамент, а не полный аналог PyCharm.
> Что уже есть и что запланировано — см. [docs/ROADMAP.md](docs/ROADMAP.md).

## Что умеет v0.1.0

| Область | Возможности |
|---|---|
| Редактор | AvaloniaEdit, подсветка Python (в т.ч. многострочные строки), нумерация строк, сворачивание `def`/`class`, автоотступы, автодополнение (ключевые слова, builtins, идентификаторы файла), поиск Ctrl+F, тёмная/светлая тема |
| Проект | Дерево файлов (ленивая загрузка), вкладки, панель Structure (outline из C++-ядра), мультивкладочный режим |
| Запуск | Запуск файла (F5 / Shift+F10) выбранным интерпретатором, потоковый вывод, ввод в консоль (`input()`), Stop |
| Python | По умолчанию — `<папка IDE>\Python 3.14\bin\python.exe`; можно сменить в Settings |
| Анализ | C++20-ядро: незакрытые/несоответствующие скобки, незавершённые строки, смешанные табы/пробелы; Python-мост: синтаксис через `ast` + ruff/pyflakes, если установлены |
| Пакеты | GUI для pip: список, установка, удаление |
| Git | status, commit, pull, push, log (через git CLI), ветка в статус-баре |

## Структура

```
core/            C++20 ядро (CMake): сканер Python, outline, диагностика, C ABI
src/ISI.IDE/     C# / .NET 8 / Avalonia 11 — GUI (MVVM, CommunityToolkit.Mvvm)
python/isi_tools Python-мост (проверка кода, линтеры)
.github/         GitHub Actions: сборка 5 архивов
docs/            архитектура, дорожная карта, Plugin API, брендинг
```

## Сборка

Требуется: CMake ≥ 3.20, компилятор C++20 (MSVC 2022 / GCC 11+ / Clang 14+), .NET SDK 8.

```bash
./build.sh [rid]                  # Linux/macOS: linux-x64 | linux-arm | osx-x64 | osx-arm64
.\build.ps1 -Rid win-x64          # Windows:     win-x64 | win-x86
```

Результат: `dist/<rid>/ISI IDE for Python/`.

### GitHub Actions

Workflow `.github/workflows/build.yml` собирает 5 архивов:

| Артефакт | RID | Формат |
|---|---|---|
| `windows-x64` | win-x64 | zip |
| `windows-x86` | win-x86 (32-bit) | zip |
| `linux-x64` | linux-x64 | tar.gz |
| `linux-arm32` | linux-arm (32-bit ARM) | tar.gz |
| `macos-x64` | osx-x64 | tar.gz |

> **Про «Linux 32-bit».** Для 32-битного **x86** Linux у .NET нет рантайма (и у Avalonia тоже),
> поэтому такой сборки получить нельзя. Слот «Linux 32» занят 32-битным **ARM** (`linux-arm`,
> например Raspberry Pi OS 32-bit). Подробности — в [ARCHITECTURE.md](ARCHITECTURE.md).

Тег `v*` (например `v0.1.0`) дополнительно создаёт GitHub Release с этими файлами.

## Встроенный Python

Положите дистрибутив Python 3.14 в папку `Python 3.14` рядом с исполняемым файлом IDE, чтобы
интерпретатор лежал по пути:

```
C:\Program Files\Wharner APP\ISI IDE for Python\Python 3.14\bin\python.exe
```

Это путь по умолчанию. Сменить — **Tools → Settings → Python interpreter**. Настройки хранятся
в профиле пользователя (`%APPDATA%\Wharner APP\ISI IDE for Python\settings.json`), потому что
`Program Files` недоступен для записи.

Если интерпретатор не найден, IDE попробует `python`/`python3` из `PATH`.

## macOS

Сборка не подписана. После распаковки: `xattr -dr com.apple.quarantine "ISI IDE for Python"`.

## Лицензия

Проприетарная, © Wharner Group. См. [LICENSE](LICENSE).
