# Запуск Office из главного меню и безопасный возврат

- Владелец: `ИИ/интегратор`
- Статус: `Done`
- Приоритет: `P0`
- Ветка: `feature/office-main-menu-flow`
- Зависимости: `office M4`, общий HUD, HotelArrival
- Лимит времени: `1 час`

## Цель

Подтвердить и закрепить сквозной путь второго персонажа из главного меню, а также
дать игроку однозначный безопасный возврат из Office и HotelArrival в `Main` без
потери checkpoint и без продолжения в пустой сцене прибытия.

## Контекст

Актуальный `main` уже содержит переход `Main -> CharacterSelect ->
Prologue_Office` и общий HUD в игровом эпизоде. `HotelArrivalController` имеет
только кнопку возврата к выбору истории, тогда как общий HUD также считает эту
сцену gameplay-сценой. Результат прибытия хранится в runtime-статике, поэтому
возврат из гостиницы через HUD оставляет неоднозначный Continue.

## Критерии готовности

- [x] Новая игра из `Main` позволяет выбрать героя 2 и загружает `Prologue_Office`.
- [x] Возврат из Office через глобальный HUD открывает `Main`; Continue ведёт к
      последнему устойчивому checkpoint Office.
- [x] `HotelArrival` показывает отдельные кнопки к выбору историй и в `Main`.
- [x] Оба выхода из HotelArrival финализируют прибытие и оставляют Continue на
      `CharacterSelect`, а не на `HotelArrival`.
- [x] Глобальный HUD скрыт в HotelArrival, чтобы не существовал обход контракта.
- [x] Unity Console чиста, flow проверен в Play Mode.
- [x] Контракты, STATE, roadmap и Handoff обновлены.

## Разрешённая область

- `Assets/Game/Core/Flow/GameFlowService.cs`
- `Assets/Game/Core/Flow/HotelArrivalController.cs`
- `Assets/Game/Core/UI/GlobalHudController.cs`
- `docs/jam/BACKLOG.md`, `CONTRACTS.md`, `STATE.md`, `OFFICE_ROADMAP.md`
- `docs/jam/tasks/office-main-menu-flow.md`

## Не менять

- Сцену и gameplay-код Office, input asset, episode payload и vendor-каталоги.
- Сцены `Main`, `CharacterSelect`, `Prologue_Office`, `HotelArrival` и Unity YAML.
- Правила завершения сюжетной линии и разблокировки Finale.

## Как проверить

1. Запустить `Main`, выбрать «Новая игра», затем героя 2.
2. В `Prologue_Office` открыть pause-меню и выйти в `Main`; Continue должен
   вернуть к устойчивой точке Office.
3. Передать валидный Office `EpisodeResult` в `HotelArrival`.
4. Проверить отдельно обе кнопки: возврат к выбору и возврат в `Main`.
5. После возврата в `Main` нажать Continue: открывается `CharacterSelect`.

## Handoff

- Что сделано: подтверждён существующий запуск Office из Main; HotelArrival получил
  отдельные кнопки в выбор историй и главное меню; общий HUD скрыт в гостинице;
  `FinishArrivalToMainMenu` очищает runtime-результат и оставляет Continue на
  `CharacterSelect`.
- Что осталось: Windows x64 build smoke и замер длительности остаются в M5.
- Известные проблемы: HotelArrival всё ещё текстовая заглушка без финального арта
  и звука; это не блокирует навигацию.
- Как проверено: controlled Play Mode прошёл `Main -> CharacterSelect ->
  Prologue_Office` (build index 4), возврат из Office через HUD (`Continue =
  Prologue_Office`), оба выхода HotelArrival и повторный Continue (`CharacterSelect`);
  Console 0 errors/warnings, EditMode tests `1/1`. Исходный пользовательский save
  восстановлен после теста.
- Последний commit реализации: `2536da7`
