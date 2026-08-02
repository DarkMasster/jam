# Базовая звуковая система

- Владелец: `ИИ/интегратор`
- Статус: `Done`
- Приоритет: `P1`
- Ветка: `feature/audio-foundation`
- Зависимости: `GameEntryPoint`, `GlobalHudController`, `CutsceneDirector`, NodeCanvas 3.42
- Лимит времени: `2 часа`

## Цель

Общий persistent-аудиосервис доступен из всех режимов, хранит пользовательские
громкости отдельно от игрового save и согласованно приглушает звук во время pause
и cutscene.

## Контекст

`GameEntryPoint` уже создаёт persistent HUD и `CutsceneDirector`. Storyboard пока
проигрывает voice через локальный `AudioSource`; эпизоды не имеют общего контракта
для музыки и SFX. В проекте пока нет production-аудиоконтента и настроенного
AudioMixer asset, поэтому runtime обязан иметь рабочий fallback без ассета.

## Критерии готовности

- [x] `GameEntryPoint` создаёт один `AudioService`, переживающий смену сцен.
- [x] Доступны шины `Master`, `Music`, `Sfx`, `UI`, `Ambience`, `Voice` и cue-ассеты.
- [x] Громкости сохраняются в `PlayerPrefs` отдельно от `jam.save.v1`.
- [x] Музыка поддерживает loop и crossfade, SFX — cooldown и лимит экземпляров.
- [x] Pause имеет приоритет над cutscene и корректно восстанавливает микс.
- [x] Storyboard voice и NodeCanvas используют проектный аудиоконтракт.
- [x] Unity компилирует изменения без новых ошибок.
- [x] Документация памяти и Handoff обновлены.

## Разрешённая область

- `Assets/Game/Core/Audio/**`
- `Assets/Game/Core/EntryPoint/GameEntryPoint.cs`
- `Assets/Game/Core/UI/GlobalHudController.cs`
- `Assets/Game/Core/Cutscenes/CutsceneDirector.cs`
- `Assets/Game/Core/Cutscenes/UiStoryboardPresentation.cs`
- `Assets/Game/Integrations/NodeCanvas/*Audio*.cs`
- `docs/jam/BACKLOG.md`, `CONTRACTS.md`, `STATE.md`, `DECISIONS.md`
- `docs/jam/tasks/audio-foundation.md`

## Не менять

- Сцены, input asset, игровые сохранения и episode-local state.
- `Assets/ParadoxNotion/**`, `Assets/DamageNumbersPro/**` и другие vendor-каталоги.
- Существующие AudioSource офисного эпизода и его roadmap в этой задаче.

## Как проверить

1. Дождаться завершения компиляции Unity и проверить Console.
2. Запустить `Main`, открыть новую игру и игровую сцену.
3. Убедиться, что существует ровно один `AudioService` после смены сцены.
4. Открыть pause-menu: активный контекст должен стать `Paused`; после закрытия —
   вернуться к `Default` либо `Cutscene`.
5. Запустить storyboard и убедиться, что voice маршрутизируется через общий сервис.

## Handoff

- Что сделано: persistent `AudioService`, логические шины и настройки, pool SFX,
  crossfade музыки, voice, cue/configuration assets, приоритетные mix-context,
  интеграция HUD/cutscene/storyboard и три NodeCanvas task.
- Что осталось: создать production `AudioMixer`/snapshots и реальные cue/клипы;
  добавить экран настроек громкости отдельной задачей.
- Известные проблемы: без `Resources/Audio/AudioConfiguration.asset` используется
  намеренный fallback через громкости `AudioSource`; контент в задачу не входил.
- Как проверено: Unity 6000.5.3f1 скомпилировала скрипты без errors/warnings;
  Play Mode smoke подтвердил один сервис, cue/handle, cooldown/concurrency,
  music/voice, round-trip громкости и цепочку
  `Default -> Cutscene -> Paused -> Cutscene -> Default`; EditMode tests `1/1`.
- Последний commit: `не выполнялся`
