# Localization + TextMeshPro

- Владелец: `интегратор`
- Статус: `Handoff`
- Приоритет: `P0`
- Ветка: `feature/localization-tmp`
- Зависимости: `Unity Localization`, `Addressables`, `TextMeshPro`

## Цель

Единая локализация RU/EN и TMP для общего UI и существующих white-box эпизодов.

## Handoff

- Что сделано: Unity Localization 1.5.12, `ru/en/qps-ploc`, таблицы
  `Common/Photo/Office`, runtime service, language toggle, TMP-миграция,
  storyboard и NodeCanvas adapters.
- Что осталось: переводить будущий контент по контракту; визуально проверить
  `qps-ploc` на целевых разрешениях.
- Известные проблемы: первый синхронный прогрев новой эпизодной таблицы в Editor
  может занять несколько секунд; в build таблицы входят через Addressables.
- Как проверено: Main, Photo и Office в Play Mode; сцены validate без issues;
  Console без errors/warnings; в проверенных runtime UI `legacy Text = 0`.
- Последний commit: не создан.
