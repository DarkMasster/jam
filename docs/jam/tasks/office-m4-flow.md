# M4 — сюжет, сохранение и общий flow офиса

- Владелец: `ИИ-агент`
- Статус: `Done`
- Приоритет: `P0`
- Ветка: `feature/office-m4-flow`
- Зависимости: `M3 завершён; карта Office, Build Settings и HotelArrival выполнены в этом же срезе`
- Лимит времени: `2 часа`

## Цель

Замкнуть офисный эпизод на общий flow: короткий Setup сна, пробуждение после
неизбежного удара, устойчивый checkpoint с episode-owned payload и один
`EpisodeResult`, после которого игрок попадает в `HotelArrival` и возвращается в
выбор героя.

## Контекст

`M0–M3` дают полный episode-local путь: маршрут, автоподбор, бросок, Momentum,
мягкий restart, Reflection beat, гарантированный сбор вещей и составного босса,
который завершает забег сюжетным ударом. До этого среза `OfficeRunController`
только помечал `IsStoryCompleted`, эпизод запускался прямой загрузкой сцены, а
`HotelArrival` и общий `EpisodeResult` не существовали.

## Критерии готовности

- [x] Setup объясняет сон в машине тремя пропускаемыми кадрами, а не длинной
      непрерываемой cutscene.
- [x] После финального удара герой просыпается и переходит в `HotelArrival`.
- [x] Checkpoint хранит только устойчивую фазу (`office.setup`, `office.run`,
      `office.arrival`) и episode-owned payload.
- [x] Результат содержит retries, `hasLaptop`, `hasMug`, а также разрушенную
      технику и списанные кресла.
- [x] Эпизод сообщает завершение через общий flow и не загружает чужую сцену
      напрямую.
- [x] `M0–M3` не регрессировали, сцена проходит validation, Console чиста.
- [x] `OFFICE_ROADMAP.md` обновлён до `Handoff`.

## Разрешённая область

- `Assets/Game/Episodes/Office/**`, `Assets/Game/Core/Flow/**`,
  `Assets/Game/Scenes/Prologue_Office.unity`, `Assets/Game/Scenes/HotelArrival.unity`,
  `Assets/Game/Core/Localization/Editor/LocalizationSetup.cs`,
  `Assets/InputSystem_Actions.inputactions`, `ProjectSettings/EditorBuildSettings.asset`,
  `docs/jam/**`.

## Не менять

- Photo- и Drive-эпизоды, их сцены и payload.
- `GameSaveService` API: срез только использует существующие методы.

## Как проверить

1. Открыть `Assets/Game/Scenes/Main.unity`, запустить Play Mode, «Новая игра».
2. В `CharacterSelect` выбрать офисную линию: загружается `Prologue_Office` и сразу
   идёт Setup из трёх кадров; `Escape` или клик пропускают его.
3. После Setup управление возвращается, сохраняется checkpoint `office.run`.
4. Пройти маршрут, собрать ноутбук и кружку, довести босса до финального удара.
5. Через ~2 секунды после удара показывается пробуждение; после него открывается
   `HotelArrival` с маршрутом, текстом и строками итога.
6. `ВЕРНУТЬСЯ К ВЫБОРУ ИСТОРИИ` возвращает в `CharacterSelect`; повторный выбор
   офиса снова открывает `Prologue_Office` с начала, а не экран прибытия.

## Handoff

- Что сделано: добавлен общий слой flow (`EpisodeResult`, `GameFlowService`,
  `HotelArrivalController`, сцена `HotelArrival` и её builder), episode-owned
  payload (`OfficeCharacterSaveData`, `OfficeCheckpointAdapter`) и
  `OfficeStoryDirector` с Setup, пробуждением, checkpoint и ручным сохранением
  через `IGameModeSaveProvider`. Созданы storyboard-ассеты `office.prologue.setup`
  и `office.prologue.awakening`. В общий input asset добавлена карта `Office`
  (Move, Aim, Primary, Secondary, Interact), офис переключён с временных
  `Player/Move` и `Player/Attack`. `Prologue_Office` и `HotelArrival` добавлены в
  Build Settings.
- Что осталось: `M5` — Windows x64 smoke test и баланс длительности 5–10 минут;
  `M6` — SFX, частицы, дрожь камеры и свет по Momentum.
- Известные проблемы: сцена прибытия — общая заглушка на тексте, без арта и
  звука; `LeaveCharacterLine` не выставляет `CompletedCharacters`, поэтому счётчик
  `0/3` в `CharacterSelect` по-прежнему растёт только после финала линии (то же
  поведение, что у Photo).
- Как проверено: `Jam/Localization/Create or Update Localization`,
  `Jam/Flow/Rebuild Hotel Arrival`, `Jam/Office/Rebuild Prologue Office`; scene
  validation — 0 issues/missing scripts/broken prefabs; controlled Play Mode из
  `Main`: `Новая игра → CharacterSelect → Prologue_Office` (Setup играет,
  `office.run` сохранён с payload `retries=0`), карта `Office` активна
  (`Move`/`Primary` enabled), финальный удар → пробуждение → `HotelArrival` со
  строками `ПЕРЕЗАПУСКОВ ЗАБЕГА 2`, `НОУТБУК ЕСТЬ`, `КРУЖКА ЕСТЬ`,
  `РАЗРУШЕНО ТЕХНИКИ 1`; после возврата checkpoint снова указывает на
  `Prologue_Office` (`office.arrival`), а повторный выбор офиса стартует с `Setup`
  и попытки 1. Console — 0 ошибок, только служебные предупреждения MCP о порте.
- Последний commit: `см. историю ветки feature/office-m4-flow`
