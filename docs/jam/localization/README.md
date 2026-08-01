# Локализация

Проект использует Unity Localization `1.5.12` и TextMeshPro. Источники правды —
String Table Collections `Common`, `Photo` и `Office` в
`Assets/Game/Localization/Tables/`. Поддерживаются `ru`, `en` и тестовый
`qps-ploc`; выбранный язык хранится отдельно от игрового слота в
`PlayerPrefs` под ключом `jam.settings.locale`.

## Runtime

- `Loc.Get(table, key, fallback, args)` — единая точка чтения строк.
- `Loc.SetLocale(code)` и `Loc.ToggleRussianEnglish()` меняют язык и уведомляют UI.
- `LocalizedTextBinding` подходит для статического TMP-текста.
- Динамический текст обновляется обработчиком `Loc.LocaleChanged`.
- Fallback обязателен: при повреждённой таблице игрок должен увидеть русский текст,
  а не пустой интерфейс.

Новый UI создаётся только через `TMP_Text`, `TextMeshProUGUI` или
`TextMeshPro`. `UnityEngine.UI.Text` и `TextMesh` не добавляются.

## Добавление текста

1. Выбрать таблицу по владельцу: общий UI — `Common`, эпизод — его таблица.
2. Добавить стабильный ключ и обе переведённые строки в `LocalizationSetup`.
3. Выполнить `Jam/Localization/Create or Update Localization` в Edit Mode.
4. Привязать TMP через `LocalizedTextBinding` либо вызвать `Loc.Get`.
5. Проверить `ru`, `en` и `qps-ploc`, отсутствие `No translation found` и
   переполнения TMP-полей.

Таблицы генерируются идемпотентно и коммитятся вместе с кодом. Ручное изменение
сгенерированных таблиц без синхронного обновления `LocalizationSetup` запрещено:
следующий запуск генератора его перезапишет.

## NodeCanvas

`GetLocalizedStringTask` читает строку в blackboard, `SetLocaleTask` меняет язык.
Граф хранит ключ, таблицу и аргументы, но не копию перевода. Диалоговые реплики
следуют ключам `<episode>.<act>.<sequence>.<entry>.(speaker|text)`.

