# Photo production Dialogue Trees и AudioCue

- Владелец: `ИИ-агент / команда Photo`
- Статус: `Doing`
- Приоритет: `P1`
- Ветка: `feature/photo-dialogue-audio`
- Зависимости: Photo production slice, NodeCanvas 3.42, общий AudioService

## Цель

Перевести ключевые разговоры фотопролога на реальные NodeCanvas Dialogue Tree
assets и подключить слышимые project-owned AudioCue без изменения save-контракта.

## Критерии готовности

- [x] Разговор с матерью использует Dialogue Tree statement/choice/finish.
- [x] Паспортный контроль использует Dialogue Tree statement/choice/finish.
- [x] TMP presentation обрабатывает subtitle и multiple-choice events NodeCanvas.
- [x] Выборы применяются через `PhotoPrologueRules`, а не через graph/save.
- [x] Подключены AudioCue комнаты, аэропорта, затвора, двери и штампа.
- [ ] Оба дерева пройдены вручную из `Main` по всем доступным веткам.

## Разрешённая область

- `Assets/Game/Episodes/Photo/**`
- `Assets/Game/Scenes/Prologue_Photo.unity`
- Photo-строки локализации и связанные `docs/jam/**`

## Handoff

- Что сделано: два Dialogue Tree assets, адаптация существующего TMP UI к
  NodeCanvas events, пять AudioCue и локальные прототипные WAV.
- Как проверено: Unity compilation и script validation без ошибок/предупреждений;
  созданные graph/cue assets привязаны в `Prologue_Photo`; сцена запускается в
  Play Mode без ошибок Console.
- Что осталось: ручной прогон обоих деревьев из `Main`, замена прототипных WAV на
  финальный саунд-дизайн и добавление AudioCue во вступительный storyboard.
- Известные ограничения: звуки синтетические и предназначены только для проверки
  пайплайна; финальные аудиофайлы не входят в текущий scope.
