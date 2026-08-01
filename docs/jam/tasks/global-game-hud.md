# Global game HUD — handoff

Статус: реализовано и проверено в `feature/global-game-hud`, ожидает review/commit.

## Контракт

- `GameEntryPoint` существует при запуске любой сцены и владеет persistent HUD.
- В `Main` и `CharacterSelect` HUD скрыт; в остальных сценах доступна кнопка
  `GlobalMenuButton` и клавиша `Escape`.
- Pause overlay: `ResumeButton`, `SaveGameButton`, `ExitToMainButton`.
- `IGameModeSaveProvider` реализуется контроллером режима и самостоятельно
  преобразует runtime-state в checkpoint/payload.
- `GameModeSaveService` является единственной точкой вызова ручного сохранения из
  общего UI. Несколько активных provider в одной сцене считаются ошибкой интеграции.
- Выход в Main не сохраняет автоматически: Continue остаётся на последнем
  checkpoint, созданном provider.

## Photo

- `PhotoWhiteboxController` реализует `IGameModeSaveProvider`.
- Фазы сопоставлены с `photo.intro`, `photo.explore`, `photo.camera`,
  `photo.published`, `photo.arrival`.
- В `PhotoPrologueProgress.introIndex` сохраняется позиция вступительного диалога.

## Проверено

- Unity 6000.5.3f1: компиляция без ошибок; новые скрипты прошли validation.
- `Main`: persistent HUD существует, но Canvas скрыт.
- `Main → New Game → Photo`: HUD видим; `GlobalMenuButton` открыл overlay,
  `Time.timeScale` изменился `1 → 0`, кнопка сохранения активна.
- `SaveGameButton`: создан `photo.intro`, payload содержит `introIndex`, статус UI
  показывает успешное сохранение.
- `ExitToMainButton`: загружен `Main`, `Time.timeScale` восстановлен в `1`,
  Continue-сцена осталась `Prologue_Photo`.
- `ContinueButton`: восстановлены `Prologue_Photo`, `IntroDialogue`, `introIndex=0`.
- Прямой запуск `Prologue_Photo`: runtime bootstrap создал `GameEntryPoint` и HUD;
  открытие/закрытие overlay восстановило `Time.timeScale 0 → 1`.
- После smoke-теста исходный PlayerPrefs-слот восстановлен; Unity Console: 0 errors.
