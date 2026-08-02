# Art-pass офиса на POLYGON Office

- Статус: Doing
- Приоритет: P1
- Владелец: ИИ-агент
- Ветка: `polish/office-synty-art-pass`
- Зависимости: `feature/office-assets-import` (vendor-каталог `Assets/PolygonOffice`)

## Цель

Заменить greybox-визуалы офисного эпизода моделями Synty POLYGON Office, не меняя
gameplay-геометрию, коллайдеры, маршрут, триггеры и vendor-каталог. Первый шаг —
проверочный срез из `OFFICE_ROADMAP.md`: стартовый кабинет, одна пара столов open
space, одна секция стекла переговорной и две серверные стойки.

## Scope

- `Assets/Game/Episodes/Office/Editor/OfficeArtPass.cs`
- `Assets/Game/Episodes/Office/Editor/OfficeSceneBuilder.cs`
- `Assets/Game/Episodes/Office/Art/Materials/M_Synty_*.mat`
- `Assets/Game/Episodes/Office/Captures/office-artpass-*.png`
- `Assets/Game/Scenes/Prologue_Office.unity`
- `docs/jam/OFFICE_ROADMAP.md`, `STATE.md`, `BACKLOG.md`, `INTEGRATIONS.md`
- этот файл задачи

Вне scope: `Assets/PolygonOffice/**`, маршрут, коллайдеры, механика, персонажи
пака, остальные зоны маршрута, предметы, противники и стойки босса.

## Критерии готовности проверочного среза

- [x] Vendor-модели подключены только как visual children project-owned объектов.
- [x] Коллайдеры, триггеры и компоненты gameplay-объектов не изменились.
- [x] Габариты visual совпадают с greybox-объёмом, под который настраивался забег.
- [x] Коллайдеры vendor-моделей выключены; столкновения остаются за greybox.
- [x] Материалы пака работают в URP и приведены к офисной палитре project-owned
      вариантами без правки vendor-каталога.
- [x] Проходы, line of sight и top-down читаемость не ухудшились.
- [x] Scene validation и Play Mode проходят без новых ошибок и предупреждений.
- [x] Оставшийся объём переноса расписан по слайсам `A2`–`A7` с точными объектами и
      количествами; зафиксирован список объектов, которые art-pass не трогает.
- [ ] `A2` open space: 4 стола, 4 кресла, 12 фоновых столов, 12 колонн.
- [ ] `A3` переговорная: правая секция стекла, 4 дверные секции, стол и 4 кресла.
- [ ] `A4` серверная: 10 оставшихся стоек.
- [ ] `A5` рецепция и EXIT: 2 стойки, порог, дверь, пилоны и притолока.
- [ ] `A6` оболочка этажа: пол по зонам, стены, потолок как новый visual-слой.
- [ ] `A7` интерактив, враги и босс: 8 клавиатур, 4 принтера, ноутбук, кружка,
      4 кресла-противника, 12 стоек босса + повторный run/restart/boss flow.

## Handoff

- Что сделано: добавлен editor-слой `OfficeArtPass` — он подключает модели пака как
  visual children и подгоняет каждую под явно указанный greybox-объём, гасит
  greybox-рендереры, выключает vendor-коллайдеры и переводит материалы пака в
  офисную палитру project-owned material variants. `OfficeSceneBuilder` собирает
  ссылки на объекты среза во время сборки, поэтому art-pass не ищет greybox по
  именам. Заменены: стартовый кабинет (стол, кресло, два шкафа, лампа), пара столов
  и кресел open space на `z = -15`, левая секция стекла переговорной (три модуля) и
  две серверные стойки `x = ±3.6, z = 9.5`.
- Файлы изменены: `OfficeArtPass.cs` (новый), `OfficeSceneBuilder.cs`, три
  `M_Synty_*.mat`, пять captures, `Prologue_Office.unity`, `OFFICE_ROADMAP.md`,
  `STATE.md`, `BACKLOG.md`, `INTEGRATIONS.md`, этот файл.
- Как проверено: см. `OFFICE_ROADMAP.md`, история изменений за 2026-08-02.
- Что осталось: остальные зоны маршрута, предметы, противники и стойки босса;
  решение по `Reflection Panel` после art-pass правой секции стекла.
- Последний commit: commit не поручался.
