# Production-пролог персонажа 3

- Владелец: `ИИ-агент / команда Photo`
- Статус: `Doing`
- Приоритет: `P0`
- Ветка: `feature/photo-prologue`
- Зависимости: Photo save v2, white-box, HUD, локализация, катсцены
- Лимит времени: `8–12 часов MVP`

## Цель

Заменить оставшиеся white-box-экраны фотопролога на трёхсценовый игровой маршрут
Комната → Подъезд → Аэропорт с Честностью/Признанием, тремя путями, сохранением и
возвратом через общий flow.

## Критерии готовности

- [x] Сцена 1 содержит секрет, первый снимок и разговор с матерью.
- [x] Сцена 2 содержит поиск повестки/бабочки и три публикации.
- [x] Сцена 3 содержит пропускаемый снимок, паспортный контроль и итог.
- [x] `ProloguePath` авторитетно определяет доступные реплики.
- [x] Save schema v3 мигрирует существующие payload v1/v2.
- [x] Новая игра и продолжение проверены из `Main`.
- [x] Все новые строки локализованы и используют TMP.
- [x] Console не содержит новых ошибок.

## Разрешённая область

- `Assets/Game/Episodes/Photo/**`
- `Assets/Game/Localization/Tables/Photo*`
- `Assets/Game/Scenes/Prologue_Photo.unity`
- связанные `docs/jam/**`

## Не менять

- Vendor-каталоги.
- Core API, общие сцены, Input Actions и другие эпизоды без отдельного согласования.

## Как проверить

Запустить `Main`, начать новую игру за персонажа 3, пройти три пути, проверить
ручное сохранение и продолжение минимум в одной точке каждой сцены.

## Handoff

- Что сделано: schema v3, правила трёх сцен, runtime TMP-flow, ru/en-локализация,
  финальный storyboard «Продолжение следует» и возврат в `CharacterSelect` через
  общий `GameFlowService`.
- Что осталось: два крайних пути, финальный layout по мокапам, AudioCue и
  production Dialogue Trees.
- Известные проблемы: текущий UI остаётся программным white-box.
- Как проверено: Unity compilation без ошибок; rules/round-trip/v2 migration smoke;
  Play Mode Balance-state с `airportPhotoTaken=false` запускает
  `photo.prologue.to_be_continued`; пропуск возвращает в `CharacterSelect`, очищает
  активную линию и сохраняет завершённый payload в `photo.arrival`. Отдельно
  проверены `Main → Новая игра → CharacterSelect → Photo` и Continue из
  `photo.explore`, `photo.camera`, `photo.published`; исходный пользовательский save
  после теста восстановлен. Console чиста.
- Реализационный commit: `0f25faf`
