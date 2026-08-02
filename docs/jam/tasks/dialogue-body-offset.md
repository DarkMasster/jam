# Dialogue body portrait offset

## Scope

- Убрать пересечение текста `DialoguePanel/Body` с левым 3D-портретом.
- Не менять размер и положение `RenderedPortrait`.

## Acceptance

- [x] Левая граница Body начинается правее портретной зоны.
- [x] Storyboard визуально проверен в Play Mode.

## Handoff

- Исправление сделано в общем `UiStoryboardPresentation`, поэтому применяется к
  экспозиционным катсценам всех персонажей.
- Изменены только anchors Body; speaker, progress и портрет не затронуты.
- Проверено на вступительном storyboard Photo: текст начинается справа от портрета,
  Console не содержит ошибок.
- У `LiberationSans SDF - Fallback` отключён `Clear Dynamic Data On Build`, чтобы
  Unity не удалял дополнительные атласы и не создавал ошибку `atlasIndex`.
