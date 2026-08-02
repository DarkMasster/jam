# Задача: `M8` расширенная разрушаемость офиса

- Ветка: `feature/office-m8-destructibility` (создана от `main`, коммитов нет)
- Статус: Done
- Roadmap: `docs/jam/OFFICE_ROADMAP.md`, раздел «M8 — Расширенная разрушаемость
  маршрута» — читать целиком перед продолжением

## Что просил продюсер

«Добавь разрушаемость для объектов» — всё вместе: мониторы, стекло переговорной,
серверные стойки, столы и шкафы. Сначала был согласован план в roadmap, затем дана
команда «давай сразу реализацию».

## Решения продюсера

1. Счётчик целей HUD: цели — только принтеры (4) и серверные стойки (12) = `16`.
   Остальное даёт Momentum и popup, но не двигает счётчик
   (`OfficeBreakable.countsAsObjective`).
2. Все broken-состояния становятся проходимыми, включая стекло переговорной;
   новый проход через переговорную разрешён явно.
3. Вклад в Momentum по классам: монитор `0.08`, стол/шкаф/рецепция `0.14`, стойка
   `0.20`, стекло `0.24`, принтер без изменений (`0.26` по умолчанию).
4. Таран переносит поведение веб-демо: реальная planar velocity героя усиливается
   текущим Momentum, успешное разрушение сохраняет `90%` скорости.

## Ключевое архитектурное решение

Принтер работает по схеме «root владеет единственным коллайдером, состояния —
чистые визуальные группы, `SetActive`». Для мебели маршрута эта схема не годится:
под ней лежат авторитетные greybox-коллайдеры (`Top`, 4 `Leg`, `Monitor`), и
выключение группы убрало бы их.

Поэтому в `OfficeBreakable` добавлена вторая схема: `intactVisualRoot` — отдельно
собираются рендереры и принадлежащие объекту коллайдеры. Исходное состояние каждого
запоминается в `Awake`: art-pass уже погасил часть greybox-рендереров, а вложенный
монитор владеет собственным коллайдером независимо от стола.

Разделение владения рендерерами между вложенными разрушаемыми объектами (монитор
внутри стола) — через `OwnsRenderer`: рендерер принадлежит тому
`OfficeBreakable`, который вернёт `GetComponentInParent`.

## Сделано

### Runtime

- `OfficeMomentum.AddBreak(float gain)` — перегрузка с собственным вкладом;
  неположительный `gain` = значение по умолчанию.
- `OfficeBreakable` переписан: поля `intactVisualRoot`, `extraIntactVisuals`,
  `momentumGain`, `countsAsObjective`; методы
  `ConfigureVisualState`, `RegisterExtraVisual`, `SetImpactFlash`; сбор и
  восстановление рендереров и коллайдеров; регистрация цели только при
  `countsAsObjective`. Broken-состояние любого объекта проходимо.
- `OfficePlayerController.OnControllerColliderHit` передаёт накопленную скорость в
  `OfficeBreakable`: `speed × (1 + Momentum × 0.4)`, после успеха остаётся `90%`.
- `OfficeEpisodeController.ReportBreakableDestroyed(string)` — статус без счётчика;
  в `LocalizeRuntimeName` добавлены шесть новых имён.
- `LocalizationSetup`: ключи `item.monitor`, `item.server_rack`, `item.desk`,
  `item.cabinet`, `item.reception_desk`, `item.meeting_glass`,
  `status.destroyed_extra` (RU/EN).

### Builder (`OfficeSceneBuilder`)

- Константы порогов и вкладов `M8` вверху класса.
- `BuildDeskTemplate`: `Monitor` из масштабированного куба стал группой с единичным
  масштабом (коллайдер `1.25 × 0.72 × 0.08` переехал с куба на группу — габарит и
  число коллайдеров те же), внутри `Screen` + `Monitor Glow` + группа `Broken`.
  Добавлена группа `Broken` самого стола. Два `OfficeBreakable`: монитор и стол.
  Фоновый `Desk_Background` получает ту же структуру, но без компонентов.
- `BuildServerRackTemplate(p, breakable)` разделён на два prefab:
  `ServerRack` (стойки босса, без изменений) и `ServerRack_Breakable` (12 стоек
  серверной). Это закрывает найденный дефект: общий шаблон позволил бы сломать
  стойки босса до сборки.
- `CreateBossRack` — новый метод, боссу выдаётся неразрушаемый шаблон.
- `BuildReceptionDeskTemplate`: группа `Broken` + компонент.
- `MakeScaledPrimitiveBreakable` + `BuildBrokenCabinet` + `BuildBrokenGlass`:
  разрушаемость масштабированных примитивов (2 шкафа, 2 секции стекла) через
  соседнюю группу обломков в мировых координатах.
- `Desk`, `Desk_Background`, `ReceptionDesk` переведены с `GetOrCreatePrefab` на
  `RebuildPrefabOnce`: изменённая структура попадает в prefab с диска ровно один
  раз за rebuild. Это устраняет найденный сброс ранних instance в `(0,0,0)` при
  повторной перезаписи одного prefab внутри цикла.

## Handoff

### Что сделано

- Завершены `D1`–`D4`: в сцене `36` разрушаемых объектов, из них `16` входят в HUD.
- `WireBreakables` вызывается после создания контроллеров; art-pass сохраняет
  `Broken`, помещает мониторный visual внутрь `Monitor` и регистрирует внешние
  vendor-визуалы шкафов и стекла.
- Broken-состояния отключают принадлежащие объекту коллайдеры и становятся
  проходимыми. Restart восстанавливает рендереры, vendor-визуалы и коллайдеры.
- Разрушение работает от броска и от накопленного разгона героя; стойка требует
  Momentum, лёгкие объекты доступны на базовой скорости.
- Стойки босса используют отдельный неразрушаемый `ServerRack`; повторный prefab
  rebuild больше не сбрасывает ранние instance в начало координат.

### Проверка

- Compile и script validation: `0` errors; scene validation: `0 issues`.
- Полный доступный EditMode-набор: `1/1 passed`; Office-specific test assembly в
  проекте отсутствует.
- Edit Mode: `36` breakable, `16` objectives, `171/171` colliders, vendor colliders
  `0`, untinted vendor materials `0`, boss breakables `0`, desk colliders `6`,
  monitor bounds `1.25 × 0.72 × 0.08`, misplaced loop instance `0`.
- Controlled Play Mode: weak/strong/repeat `false/true/false`; все `36` объектов
  ломаются, runtime colliders `159 → 95 → 159`, restart оставляет `0` broken.
- Внешние vendor-визуалы шкафа: `2 → 0` при разрушении и восстановление после reset.
- Реальный таран стекла: скорость `9.5`, проход `x 7.00 → 7.42 → 8.62`; стойка
  выдерживает базовый таран и ломается после усиления Momentum.
- `Assets/PolygonOffice/**` не изменён. Console без игровых ошибок. TMP fallback
  очищен штатным API до `1 atlas / 0 glyphs / 0 chars / 1×1`; importer warning был
  разовой гонкой `SaveAssets` с TMP post-render, после refresh и повторной
  перерисовки не воспроизводится.

### Изменённые области

- Runtime: `OfficeBreakable`, `OfficeMomentum`, `OfficeEpisodeController`,
  `OfficePlayerController`.
- Editor: `OfficeSceneBuilder`, `OfficeArtPass`, `LocalizationSetup`.
- Prefabs: `Desk`, `Desk_Background`, `Printer`, `ReceptionDesk`, новый
  `ServerRack_Breakable`; сцена `Prologue_Office` и таблицы `Office`.
- Документация: `OFFICE_ROADMAP.md`, `STATE.md`, этот task-файл.

### Осталось или заблокировано

- Полный ручной `run → restart → boss → HotelArrival` после M8 не повторялся;
  boss-prefab и boss flow не менялись, структурно `Boss Rack 01..12` не получили
  `OfficeBreakable`.
- Windows x64 build по-прежнему заблокирован отсутствующим модулем Windows Build
  Support; это внешний блокер из `STATE.md`.
- Commit не создавался.
