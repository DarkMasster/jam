# White-box истории персонажа 3

- Владелец: `ИИ/интегратор`
- Статус: `Done`
- Приоритет: `P0`
- Ветка: `feature/photo-prologue-whitebox`
- Зависимости: `Main`, `CharacterSelect`, `GameSaveService`, NodeCanvas 3.42
- Лимит времени: завершено

## Цель

Дать команде сквозной запускаемый маршрут из главного меню в историю персонажа 3 с минимальным интерактивным прохождением и продолжением с checkpoint.

## Критерии готовности

- [x] `CharacterSelect` загружает `Prologue_Photo`, а не `SampleScene`.
- [x] Сцена содержит камеру, Blackboard, FSMOwner и white-box контроллер.
- [x] Реальный NodeCanvas Asset Graph содержит семь утверждённых фаз.
- [x] Игрок проходит экспозицию, осмотр, камеру, публикацию и финал Пролога.
- [x] Выбор повестки или бабочки сохраняет `Truth`/`Reach` в JSON payload.
- [x] «Продолжить» восстанавливает безопасную фазу после остановки сессии.
- [x] Unity Console не содержит ошибок и предупреждений.

## Разрешённая область

- `Assets/Game/Episodes/Photo/**`
- `Assets/Game/Integrations/NodeCanvas/**`
- `Assets/Game/Scenes/Prologue_Photo.unity`
- `ProjectSettings/EditorBuildSettings.asset`
- связанные документы `docs/jam/**`

## Не менять

- Vendor-каталоги NodeCanvas и Damage Numbers Pro.
- Сцены и runtime-код Drive/Office.
- Общий save schema.

## Как проверить

1. Запустить `Main`.
2. Нажать «Начать новую игру» и выбрать персонажа 3.
3. Завершить экспозицию и убедиться, что открылась фаза Explore.
4. Остановить Play Mode, снова запустить `Main` и нажать «Продолжить».
5. Убедиться, что загрузилась `Prologue_Photo` в Explore с сохранённым состоянием.

## Handoff

- Что сделано: сквозной маршрут, сцена, FSM asset, Blackboard, интерактивный white-box и четыре checkpoint.
- Что осталось: production Dialogue Trees, художественная сцена, реальные области кадра и DNP-prefab каталога Photo.
- Известные проблемы: текущие реплики и UI находятся в C# как временный white-box; сцена `HotelArrival` заменена финальным экраном внутри Photo.
- Как проверено: ручной play-mode маршрут `Main -> CharacterSelect -> Photo`, остановка сессии и восстановление `photo.explore`, Unity Console без ошибок/предупреждений.
- Последний commit: не создавался; требуется отдельное явное поручение.
