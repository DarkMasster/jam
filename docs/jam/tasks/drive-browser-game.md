# Drive browser game integration

## Scope

- Включить предоставленную автономную HTML-игру первого персонажа в билд.
- Запускать её системным браузером из карточки Drive в `CharacterSelect`.
- Не ломать существующий Unity fallback `Prologue_Drive`.

## Acceptance

- [x] HTML лежит в `StreamingAssets` и включается в desktop build.
- [x] `SelectDrive` сохраняет активную линию и открывает локальный file URL.
- [x] При отсутствии файла или в WebGL используется Unity-сцена Drive.
- [ ] Завершение HTML-игры синхронизируется с Unity save-файлом.

## Handoff

- Интеграция сделана через `CharacterSelectController.TryLaunchDriveBrowserGame`.
- HTML автономен, использует встроенные CSS/JS и только внешние Google Fonts.
- Для разблокировки общего финала позже нужен отдельный bridge завершения.
