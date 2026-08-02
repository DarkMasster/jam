# Импорт визуальных ассетов Office

- Статус: Done
- Владелец: ИИ-агент
- Ветка: `feature/office-assets-import`
- Источник: `eyetengu/2024_March_Office`, commit `6034246ac9db8abc4ec5fa67d695c512cf648255`
- Зависимости: подтверждённая командой лицензия Synty POLYGON Office

## Цель

Добавить в проект исходный vendor-каталог `POLYGON Office`, сохранив Unity `.meta`,
не импортируя сцены и настройки вне vendor-каталога, внешний demo-код и
несвязанные наборы.

## Scope

- `Assets/PolygonOffice/**`
- `Assets/PolygonOffice.meta`
- `docs/jam/INTEGRATIONS.md`
- `docs/jam/STATE.md`
- `docs/jam/BACKLOG.md`
- `docs/jam/OFFICE_ROADMAP.md`
- этот файл задачи

## Критерии готовности

- [x] Каталог скопирован вместе со всеми `.meta`.
- [x] В импорт не попали сцены и настройки вне исходного vendor-каталога.
- [x] Зафиксированы источник, commit и границы vendor-каталога.
- [x] Unity завершает импорт без новых ошибок компиляции.
- [x] Зафиксирован план замены greybox-визуалов без изменения gameplay-контрактов.

## Handoff

- Что сделано: импортирован исходный `Assets/PolygonOffice` из snapshot
  `6034246`; прочие каталоги и настройки проекта-источника не переносились.
  Вложенные sample-сцены сохранены как часть неизменённого vendor snapshot, но не
  подключены к production flow. В roadmap добавлены направление art-pass, точные
  кандидаты замены и порядок внедрения.
- Файлы изменены: `Assets/PolygonOffice/**`, `Assets/PolygonOffice.meta`,
  `docs/jam/INTEGRATIONS.md`, `STATE.md`, `BACKLOG.md`, `OFFICE_ROADMAP.md` и этот
  файл задачи.
- Как проверено: source/target содержат по 4 850 файлов, `diff -qr` не нашёл
  различий; `.meta` верхнего каталога также скопирован из источника. Unity
  6000.5.6f1 завершил refresh и распознал 808 prefab'ов; Console — 0 ошибок и 7
  служебных предупреждений MCP о смене порта после domain reload.
- Что осталось: production-сцена пока не использует новые prefab'ы. Art-pass
  вынесен в отдельную будущую задачу `polish/office-synty-art-pass`.
- Последний commit: commit не поручался.
