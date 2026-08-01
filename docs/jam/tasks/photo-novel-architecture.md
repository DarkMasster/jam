# Архитектура визуальной новеллы Photo

- Владелец: `ИИ/интегратор`
- Статус: `Done`
- Приоритет: `P0`
- Ветка: `feature/photo-novel-architecture`
- Зависимости: `GameSaveService`, NodeCanvas 3.42, Damage Numbers Pro 4.55
- Лимит времени: завершено

## Цель

Актуализировать спецификацию фотопролога с учётом утверждённых паттернов vendor-ассетов, не начиная runtime-реализацию.

## Критерии готовности

- [x] Удалён план параллельного `DialogueSequence`/`DialogueRunner`.
- [x] Зафиксированы роли NodeCanvas FSM и Dialogue Trees.
- [x] Определены C#-компоненты фотомеханики и граница Blackboard.
- [x] Определены стабильные checkpoint и versioned payload.
- [x] Зафиксировано использование DNP только через feedback-слой.

## Разрешённая область

- `docs/jam/DEVELOPMENT_SPEC.md`
- `docs/jam/CONTRACTS.md`
- `docs/jam/DECISIONS.md`
- `docs/jam/BACKLOG.md`
- `docs/jam/STATE.md`
- `docs/jam/tasks/photo-novel-architecture.md`

## Не менять

- C#-код, Unity-сцены, prefab, NodeCanvas graphs и vendor-каталоги.

## Как проверить

Сверить архитектуру Photo между `DEVELOPMENT_SPEC.md`, `CONTRACTS.md` и `INTEGRATIONS.md`; выполнить `git diff --check`.

## Handoff

- Что сделано: утверждён поток `Restore -> IntroDialogue -> Explore -> Camera -> Publish -> ReflectionDialogue -> Arrival`, C#-компоненты, checkpoint и DNP-feedback.
- Что осталось: назначить владельца `feature/photo-prologue` и реализовать первый вертикальный срез.
- Известные проблемы: точные значения `Truth`/`Reach`, тексты Dialogue Trees и визуальные assets ещё не утверждены.
- Как проверено: сопоставление с текущими API `GameSaveService`, установленными NodeCanvas/DNP и общими контрактами.
- Последний commit: не создавался; требуется отдельное явное поручение.
