# Технические и командные контракты

Конкретные имена сцен, событий, компонентов и API добавляются после выбора
технологий. Здесь хранятся только договорённости, влияющие на нескольких людей.

## Владение

- У каждой активной задачи есть один владелец и одна feature-ветка.
- Общая сцена, prefab, карта, настройки проекта или другой конфликтный ресурс
  одновременно имеют одного владельца.
- Остальные участники передают владельцу готовый компонент, отдельный ассет или
  точную инструкцию интеграции.

## Изменения интерфейсов

- Публичный интерфейс между системами сначала записывается здесь.
- Переименование общей сцены, события, input action или публичного API требует
  согласования с затронутыми владельцами.
- Изменение контракта и его документации попадает в один commit.

## Старт приложения и сохранение

- `Assets/Game/Scenes/Main.unity` — сцена с build index `0` и единственная точка
  входа в игру.
- `GameEntryPoint` существует в `Main`, переживает смену сцен через
  `DontDestroyOnLoad` и не допускает дубликатов.
- «Новая игра» очищает прежний слот, создаёт прогресс и загружает
  `CharacterSelect`.
- «Продолжить» доступна только при наличии сохранённой сцены в Build Settings.
- `CharacterId`: `Drive`, `Office`, `Photo`; порядок прохождения свободный.
- Общие системы записывают простой переход через `SetLastScene`, а checkpoint
  линии — через `SaveCharacterCheckpoint(CharacterId, sceneName, checkpointId,
  payloadJson)`. Payload принадлежит эпизоду и не интерпретируется Core.
- Эпизод читает свой checkpoint через `TryGetCharacterCheckpoint` и сообщает
  завершение через `CompleteMainStoryLine` либо компонент
  `EpisodeProgressReporter`.
- `FinaleUnlocked` становится истинным только после завершения всех трёх линий.
- `CharacterSelect` имеет build index `1`; отсутствующие эпизодные сцены временно
  заменяются `SampleScene`, но выбранный `CharacterId` всё равно сохраняется.

## Ввод

- Вся игра, включая gameplay, меню и UI, использует только Unity New Input System.
- Единственный общий runtime-asset ввода — `InputSystem_Actions.inputactions`; его
  владельцем является интегратор.
- Эпизоды используют согласованные action maps и запрашивают новые actions у
  интегратора вместо создания параллельных assets.
- Legacy Input Manager, `UnityEngine.Input` и `StandaloneInputModule` запрещены.
- UI работает через `InputSystemUIInputModule`; gameplay — через `InputAction`,
  `PlayerInput` или общий адаптер ввода.
- Игровой код не привязывается напрямую к конкретной клавиатуре, мыши или геймпаду.

## Интеграция

### NodeCanvas и Damage Numbers Pro

- NodeCanvas FSM управляет фазами эпизода, Dialogue Tree — диалогами и выборами, Behaviour Tree — только поведением NPC/противников.
- Blackboard хранит временное состояние. Долговременный прогресс записывает только `GameSaveService` через смысловые checkpoint эпизода.
- Custom NodeCanvas tasks обращаются к проектным C#-сервисам и не копируют игровую логику в graph.
- Damage Numbers Pro — presentation-слой. Игровые системы и NodeCanvas вызывают проектный feedback-интерфейс, который выбирает DNP-prefab/preset.
- `Assets/ParadoxNotion/**` и `Assets/DamageNumbersPro/**` не редактируются; проектные адаптеры размещаются в `Assets/Game/Integrations/**`.
- Полные паттерны, версии и checklist находятся в `INTEGRATIONS.md`.

### Фотопролог

- `Assets/Game/Scenes/Prologue_Photo.unity` имеет build index `3`; выбор
  `CharacterId.Photo` в `CharacterSelect` загружает эту сцену без fallback.
- White-box использует Asset Graph
  `Assets/Game/Episodes/Photo/Graphs/PhotoPrologueWhitebox.asset`; его фазы
  синхронизирует `PhotoWhiteboxController` через `FSMOwner.TriggerState`.
- Единственный авторитетный runtime-state фотопролога хранит `PhotoEpisodeController`.
- NodeCanvas FSM использует фазы `Restore`, `IntroDialogue`, `Explore`, `Camera`,
  `Publish`, `ReflectionDialogue`, `Arrival`; Dialogue Trees не сохраняют прогресс.
- Стабильные checkpoint ID: `photo.explore`, `photo.camera`, `photo.published`,
  `photo.arrival`.
- `PhotoCheckpointAdapter` сериализует versioned payload и вызывает
  `GameSaveService.SaveCharacterCheckpoint`; Core не интерпретирует payload.
- `PhotoCameraController` выбирает `PhotoTarget` по пересечению областей кадра.
- Damage Numbers Pro получает только presentation-события через
  `GameFeedbackService` после изменения state.
- Существующий `EpisodeProgressReporter` не сохраняет промежуточный Photo-state
  без payload; завершение всей линии происходит только после Photo-финала.
- До реализации production-компонентов white-box контроллер временно владеет
  Photo-state и UI, но сохраняет те же checkpoint ID и JSON payload схемы `2`.
- `PhotoCharacterSaveData` хранит всю линию героини по актам: `prologue`,
  `mainAct`, `finale`; Core продолжает считать payload непрозрачным.
- `PhotoCheckpointAdapter` единолично сериализует, проверяет инварианты и
  мигрирует legacy white-box payload версии `1` в схему `2`.
- Публикация сначала атомарно фиксирует `publicationCommitted` и checkpoint,
  затем запускает presentation; повторная загрузка не начисляет эффект заново.
- Завершение Пролога выставляет только `prologue.completed`. Метод
  `CompleteMainStoryLine(Photo)` разрешён лишь после Финала линии.
- `GameSaveService.LeaveCharacterLine` возвращает в `CharacterSelect`, очищая
  активную сессию, но сохраняя последний checkpoint персонажа.

- Интегратор отвечает за `main`, общий билд и разрешение конфликтов общих
  ресурсов.
- Feature-ветка предоставляет воспроизводимый способ проверки.
- Если интеграция занимает больше 30 минут, scope задачи уменьшается или изменение
  откатывается отдельным безопасным commit после решения продюсера.

## Критерий блокирующей ошибки

Блокирующей считается ошибка, если она вызывает crash, мешает начать или закончить
сессию, ломает управление, уничтожает прогресс либо делает цель игры непонятной.
