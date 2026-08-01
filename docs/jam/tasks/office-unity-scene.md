# Первый Unity-срез офисного кошмара

- Владелец: `ИИ-агент`
- Статус: `Done`
- Приоритет: `P0`
- Ветка: `feature/office-unity-scene`
- Зависимости: `общий InputSystem_Actions; финальная карта Office принадлежит интегратору`
- Лимит времени: `2 часа`

## Цель

Создать первый запускаемый Unity-срез `Prologue_Office`: короткий читаемый маршрут
через пять офисных зон, top-down движение, автоматический сбор ноутбука и кружки и
ложную дверь `EXIT`, которая остаётся закрытой и обозначает будущую зону босса.

## Контекст

Веб-демо `web-demos/office/` остаётся референсом темпа и визуальной подачи, но не
копируется буквально. Общий input asset пока содержит карту `Player`, а
утверждённая карта `Office` ещё не создана интегратором. Первый срез использует
настраиваемую ссылку на существующие `Player/Move`, не изменяя общий asset; перед
интеграцией ссылка должна быть переключена на `Office/Move`.

## Критерии готовности

- [x] Сцена `Prologue_Office` открывается и запускается напрямую в Play Mode.
- [x] Игрок двигается по XZ через Unity New Input System; камера следует сверху.
- [x] Маршрут содержит стартовый кабинет, open space, переговорную, серверную и
      рецепцию с ложной дверью `EXIT`.
- [x] Ноутбук и кружка подбираются автоматически, а HUD показывает прогресс.
- [x] `EXIT` не выпускает игрока и до сбора предметов объясняет, чего не хватает.
- [x] Console не содержит новых ошибок или предупреждений офисного среза.
- [x] Документация памяти обновлена.
- [x] Создан живой `OFFICE_ROADMAP.md` с milestones, зависимостями, критериями и
      следующим небольшим срезом.
- [x] Обязательное чтение и редактирование roadmap для каждой офисной задачи
      закреплено в инструкциях агента, памяти и командном контракте.

## Разрешённая область

- Файлы или директории, которые можно менять: `Assets/Game/Episodes/Office/**`,
  `Assets/Game/Scenes/Prologue_Office.unity`, `docs/jam/BACKLOG.md`,
  `docs/jam/STATE.md`, `docs/jam/OFFICE_ROADMAP.md`, `docs/jam/README.md`,
  `docs/jam/CONTRACTS.md`, `docs/jam/DECISIONS.md`, корневой `AGENTS.md`, этот файл
  задачи, а также `.editorconfig`, `.gitattributes` и точечная нормализация
  `ProjectSettings/ProjectAuditorSettings.asset`, явно порученная продюсером.
- Общие ресурсы, владельцем которых является исполнитель: сцена
  `Prologue_Office` и episode-local ассеты Office.

## Не менять

- `Assets/InputSystem_Actions.inputactions`, `Assets/Game/Core/**`, другие сцены,
  Build Settings, остальные Project Settings, vendor-каталоги и публичные
  контракты.
- Финальный бой, боевой AI, разрушаемость и новые input actions не входят в этот
  первый срез.

## Как проверить

Открыть `Assets/Game/Scenes/Prologue_Office.unity`, запустить Play Mode, пройти
маршрут WASD/стрелками, автоматически подобрать ноутбук и кружку и подойти к
`EXIT`. До сбора обоих предметов HUD сообщает о недостающей цели; после сбора
дверь остаётся закрытой и сообщает, что зона босса подготовлена для следующего
среза.

## Handoff

- Что сделано: создана сцена `Prologue_Office` с пятизонным greybox-маршрутом,
  episode-local палитрой и светом; добавлены движение через New Input System,
  ортографическая follow-камера, HUD, зоны, автоматические pickups ноутбука и
  кружки, закрытый `EXIT` и воспроизводимый editor scene-builder. Дополнительно
  создан `OFFICE_ROADMAP.md`; обязательное чтение и редактирование roadmap перед
  handoff каждой офисной задачи закреплено в `AGENTS.md`, jam-README, контрактах и
  журнале решений.
- Что осталось: интегратору добавить карту `Office`, переключить ссылку с
  `Player/Move`, включить сцену в Build Settings и подключить её к
  `CharacterSelect`; следующий ограниченный офисный срез `M1A` — один переносимый
  предмет, автоматический выбор, бросок, lockout повторного подбора и одно простое
  разрушение. После него — один противник, Momentum, restart и финальный босс.
- Известные проблемы: без изменения общих ресурсов `CharacterSelect` пока ведёт в
  `SampleScene`; Windows build не выполнялся. Предупреждения MCP о повторном выборе
  порта после domain reload относятся к инструменту и не создаются сценой.
  Однострочный diff `ProjectAuditorSettings.asset` оказался повторной сериализацией
  пустого `m_Name`, а не изменением настройки; формат Unity принят как baseline и
  защищён от внешнего trimming.
- Как проверено: Unity 6000.5.6f1, direct Play Mode, движение и автоподбор обоих
  предметов, состояния EXIT при 1/2 и 2/2, физически активная дверь, HUD,
  `manage_scene validate` — 0 issues/missing scripts, C# validation без errors,
  Console после чистого прогона — 0 errors/warnings. Roadmap сверен с
  `DEVELOPMENT_SPEC.md`, `STATE.md`, текущим backlog и этим handoff; обязательное
  правило присутствует во всех заявленных точках входа. Для Unity YAML проверены
  Git-атрибуты и whitespace-check; открытый Unity Editor повторно записал
  `ProjectAuditorSettings.asset` (mtime изменился), но SHA-256 остался
  `5e40807b54a51f2fafb156c8deeba785328010ec5d296c089cfa74f146b751f4`.
- Последний commit: `commit не поручен`

## Дополнение (2026-08-01, follow-up в той же ветке)

- Что сделано: по запросу продюсера повторяющаяся мебель (`Desk`, `Chair`,
  `ServerRack`, `ReceptionDesk`, `Turnstile`) вынесена из `OfficeSceneBuilder` в
  переиспользуемые prefab-ассеты (`Assets/Game/Episodes/Office/Prefabs/`) вместо
  копирования иерархии объектов на каждый вызов; попутно перестроен силуэт
  `Turnstile` (пост с тремя лучами-вертушками вместо одиночного диагонального
  штыря). Следом HUD тоже вынесен в отдельный prefab `OfficeHud.prefab` (новый
  runtime-компонент `OfficeHudBinding` отдаёт zone/objective/status ссылки).
  Дополнительно исправлена расстановка стульев open space (стояли сбоку от
  стола с пересечением столешницы) и найдена/исправлена системная ошибка
  ориентации: спинка стула во всех местах размещения (`Start`, `Meeting N/S`,
  `Open`) стояла к столу/монитору вместо от него — все 11 стульев развёрнуты
  на 180°.
- Файлы изменены: `Assets/Game/Episodes/Office/Editor/OfficeSceneBuilder.cs`,
  новый `Assets/Game/Episodes/Office/Runtime/OfficeHudBinding.cs`, новые
  `Assets/Game/Episodes/Office/Prefabs/*.prefab(.meta)` (мебель + `OfficeHud`),
  `Assets/Game/Scenes/Prologue_Office.unity` (пересобрана), `OFFICE_ROADMAP.md`.
- Как проверено: `Jam/Office/Rebuild Prologue Office`, `manage_scene validate` —
  0 issues/missing scripts/broken prefabs, чистая Console после компиляции и
  рёбилда, Play Mode smoke test (движение, HUD, pickup, монтаж мебели) без
  ошибок, силуэт турникета сверен скриншотом сверху, ориентация стульев сверена
  вычислением в редакторе (`execute_code`): направление к спинке от
  стола/монитора у всех 11 стульев отрицательное (спинка развёрнута от стола).
- Что осталось: без изменений к предыдущему handoff (интеграция карты `Office`,
  Build Settings, `M1A`).
- Последний commit: `commit не поручен`
