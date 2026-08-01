# M5 — build-ready офисного эпизода

- Владелец: `ИИ-агент`
- Статус: `В работе`
- Приоритет: `P0`
- Ветка: `feature/office-m4-flow`
- Зависимости: `M4 завершён; Windows x64 заблокирован отсутствующим модулем сборки`
- Лимит времени: `1 час`

## Цель

Довести офисный эпизод до состояния, в котором полный путь
`Main → CharacterSelect → Prologue_Office → HotelArrival` проходится в собранном
билде без soft lock и с чистой Console.

## Контекст

`M4` уже добавил карту `Office`, Build Settings и общий flow, поэтому полный путь
проходит в Play Mode. Оставались проверка в реальной сборке, баланс длительности и
запись результата smoke test.

## Критерии готовности

- [x] `Main → CharacterSelect → Prologue_Office → HotelArrival` проходит в Play Mode.
- [x] Console чиста; сцена не содержит missing scripts/references.
- [ ] Цикл длится 5–10 минут и не допускает soft lock.
- [ ] Выполнен Windows x64 smoke test и записан результат.

## Разрешённая область

- `ProjectSettings/EditorBuildSettings.asset`, `Builds/**`, `docs/jam/**`,
  балансные значения внутри `Assets/Game/Episodes/Office/**`.

## Не менять

- Сюжетные фазы, контракты flow и содержание других эпизодов.

## Как проверить

1. Собрать standalone build из Build Settings.
2. Запустить билд, выбрать офисную линию и пройти забег до пробуждения.
3. Замерить время полного цикла и убедиться в отсутствии soft lock.

## Handoff

- Что сделано: Build Settings приведены к финальному списку `Main` (0),
  `CharacterSelect` (1), `SampleScene` (2), `Prologue_Photo` (3), `Prologue_Office`
  (4), `HotelArrival` (5). Полный путь `Main → CharacterSelect → Prologue_Office →
  HotelArrival → CharacterSelect` пройден в controlled Play Mode, сцена проходит
  validation, Console не содержит ошибок эпизода.
- Что осталось: собрать standalone-билд и пройти в нём тот же путь; замерить
  длительность цикла живым прохождением и привести её к 5–10 минутам.
- Известные проблемы:
  1. **Windows x64 недоступен на этой машине.** В Unity 6000.5.6f1 установлены
     только `MacStandaloneSupport` и `WebGLSupport`;
     `BuildPipeline.IsBuildTargetSupported(Standalone, StandaloneWindows64)`
     возвращает `False`. Чтобы закрыть критерий, нужно доустановить `Windows Build
     Support (Mono)` через Unity Hub — это действие человека, оно требует загрузки
     модуля и не выполняется из кода.
  2. **Попытка собрать macOS-билд как замену smoke test не завершилась.** Сборка
     `StandaloneOSX` в `Builds/OSX/Jam.app` была запущена, Burst отработал, после
     чего редактор перестал отвечать: MCP-мост фиксирует `Command TCS timed out
     (24 consecutive)`, процесс Unity держит ~0.8% CPU, каталог `Builds/` не создан.
     Похоже на модальное окно редактора, ожидающее нажатия. Перед следующим шагом
     человеку нужно открыть Unity, закрыть висящий диалог и повторить сборку из
     `File → Build Settings`.
  3. Длительность цикла не измерена. Оценка по маршруту (около 66 метров от старта
     до `EXIT` при скорости 10,5 м/с), боевым паузам, сборке босса и сюжетным битам
     даёт заметно меньше целевых 5–10 минут. Это вопрос баланса к продюсеру:
     удлинять маршрут или принять более короткий забег.
- Как проверено: `BuildPipeline.IsBuildTargetSupported` для `StandaloneWindows64`
  (`False`), `StandaloneOSX` (`True`), `WebGL` (`True`); список Build Settings
  подтверждён ответом `manage_build action=scenes` (6 сцен, все enabled); полный
  путь и checkpoint проверены в Play Mode (см. `office-m4-flow.md`).
- Последний commit: `см. историю ветки feature/office-m4-flow`
