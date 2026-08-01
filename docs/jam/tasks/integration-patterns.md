# Паттерны NodeCanvas и Damage Numbers Pro

- Владелец: `ИИ/интегратор`
- Статус: `Done`
- Приоритет: `P1`
- Ветка: `feature/integration-patterns`
- Зависимости: установленные NodeCanvas и Damage Numbers Pro
- Лимит времени: завершено

## Цель

Проверить установленные ассеты и зафиксировать единые паттерны их использования для параллельной разработки эпизодов.

## Критерии готовности

- [x] Версии и каталоги подтверждены по исходникам.
- [x] Публичные API проверены через Unity reflection.
- [x] Граница NodeCanvas, GameSaveService и Damage Numbers Pro зафиксирована.
- [x] Документы долговременной памяти обновлены.

## Разрешённая область

- `AGENTS.md`
- `docs/jam/README.md`
- `docs/jam/INTEGRATIONS.md`
- `docs/jam/CONTRACTS.md`
- `docs/jam/DECISIONS.md`
- `docs/jam/STATE.md`
- `docs/jam/BACKLOG.md`
- `docs/jam/tasks/integration-patterns.md`

## Не менять

- Игровой код, сцены, prefab, graph assets и vendor-каталоги.

## Как проверить

Открыть `INTEGRATIONS.md`, сверить версии и API; проверить Unity Console и `git diff --check`.

## Handoff

- Что сделано: описаны роли FSM/Dialogue Tree/BT, save-bridge, graph ownership, DNP feedback/pooling и совместная схема интеграции.
- Что осталось: создавать адаптеры и graph assets только в рамках конкретных эпизодных задач.
- Известные проблемы: интеграционные runtime-сценарии ещё не реализованы, поэтому play-mode smoke test не применим.
- Как проверено: исходники ассетов, reflection загруженных Unity-сборок, Unity Console (0 ошибок/предупреждений), проверка Markdown и git diff.
- Последний commit: не создавался; требуется отдельное явное поручение.
