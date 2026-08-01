# Cutscene foundation — handoff

Статус: реализовано и проверено в `feature/cutscene-foundation`, ожидает review/commit.

## Состав

- `CutsceneDirector` — persistent оркестратор, один на приложение.
- `ICutscenePresentation` — scene-local контракт презентации по стабильному ID.
- `StoryboardCutsceneAsset` — данные кадров UI-катсцены.
- `UiStoryboardPresentation` — показ кадров, voice, click/Space/Enter, skip.
- `TimelineCutscenePresentation` — адаптер настроенного scene `PlayableDirector`.
- `PlayCutsceneTask` — NodeCanvas action, ожидающая результат по ID.

## Поток

1. FSM/Dialogue Tree проверяет условие и вызывает `PlayCutsceneTask`.
2. Director находит presentation только в активной сцене и скрывает global HUD.
3. Presentation возвращает `Completed`, `Skipped`, `Failed` или `SceneChanged`.
4. NodeCanvas применяет последствия к episode-state.
5. Следующая отдельная task сохраняет checkpoint.

Катсцена не сохраняет gameplay-state и не хранит позицию Timeline/кадра между
сессиями. Это защищает от повторного начисления последствий.

## Проверено

- Unity 6000.5.3f1: компиляция без ошибок; NodeCanvas task компилируется против
  установленного NodeCanvas 3.42.
- Runtime bootstrap создаёт Director вместе с HUD при прямом запуске Photo-сцены.
- Двухкадровый storyboard показал оба кадра и вернул один `Completed` callback.
- Повторный запуск + skip вернул один `Skipped` callback; HUD скрывался на время
  катсцены и восстанавливался после результата.
- Отсутствующий ID вернул `false` и диагностическое сообщение без исключения.
- Timeline без playable asset вернул `Failed`; с runtime TimelineAsset и
  настроенным PlayableDirector вернул `Completed`.
- UI-слой storyboard визуально проверен в Game View: текст, speaker, progress и
  skip-кнопка читаемы поверх gameplay.
- Создан первый production-bound storyboard `photo.prologue.intro`: четыре
  экспозиционных кадра героини лежат в `PhotoIntroStoryboard.asset`, а
  `Prologue_Photo` содержит scene-local `UiStoryboardPresentation`.
- Маршрут `Main -> Новая игра -> Photo` запускает storyboard. И обычное
  завершение, и skip переводят NodeCanvas FSM в `Explore` и атомарно сохраняют
  `photo.explore`; blackboard фиксирует `Completed` либо `Skipped`.
- После smoke исходный PlayerPrefs восстановлен, временные объекты/скриншот
  удалены; Unity Console: 0 новых errors.
