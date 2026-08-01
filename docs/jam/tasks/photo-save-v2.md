# Photo save v2 — handoff

Статус: реализовано и проверено, ожидает review/commit.

- Ветка: `feature/photo-save-v2`.
- Payload: `PhotoCharacterSaveData`, `schemaVersion = 2`.
- Владение сериализацией: `PhotoCheckpointAdapter`.
- Акты: `prologue`, `mainAct`, `finale`; активный акт хранится в `activeAct`.
- Legacy: white-box payload `version = 1` мигрирует при первом чтении.
- Инварианты: камера требует `inspectedMask == 0b111`; публикация и прибытие
  требуют выбранный кадр; некорректные данные откатываются на безопасную фазу.
- Идемпотентность: `publicationCommitted` записывается до presentation, поэтому
  восстановление продолжает с рефлексии и не повторяет эффект публикации.
- Прогресс: `photo.arrival` выставляет только `prologue.completed`; флаг полного
  прохождения персонажа и разблокировка общего финала не выставляются.
- Возврат: `GameSaveService.LeaveCharacterLine` очищает активную сессию и ставит
  `CharacterSelect` как Continue-сцену, сохраняя checkpoint персонажа.

## Проверено

- Unity 6000.5.3f1: компиляция без ошибок.
- Валидация четырёх изменённых C#-скриптов: 0 errors, 0 warnings.
- Миграция v1 → v2: `photo.published` восстанавливается в `ReflectionDialogue`.
- Повторная загрузка committed-публикации: `ReflectionDialogue`, без повтора.
- Повреждённый `Arrival` без осмотра и выбора: откат в `Explore`, флаги сняты.
- Валидный завершённый Пролог: восстановление в `Arrival`.
- Возврат из линии: `active=None`, Photo не завершён, `photo.arrival` сохранён,
  Continue ведёт в `CharacterSelect`.
- Smoke «Новая игра»: `Main → NewGameButton → CharacterSelect → PhotoButton →
  Prologue_Photo`; runtime-root и UI созданы, `activeCharacter = Photo`.
- Smoke «Продолжить»: тестовый `photo.camera` сделал `ContinueButton` активным;
  маршрут `Main → ContinueButton → Prologue_Photo` восстановил фазу `Camera`.
- Перед smoke все открытые сцены сохранены. После проверки тестовый слот удалён,
  исходный `jam.save.v1` восстановлен; в Unity Console 0 errors.

Следующее расширение: при появлении сцен Основы и Финала заполнить `mainAct` и
`finale`, а `CompleteMainStoryLine(CharacterId.Photo)` вызывать только после
завершения `finale`.
