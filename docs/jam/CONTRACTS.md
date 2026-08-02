# Технические и командные контракты

Конкретные имена сцен, событий, компонентов и API добавляются после выбора
технологий. Здесь хранятся только договорённости, влияющие на нескольких людей.

## Владение

- У каждой активной задачи есть один владелец и одна feature-ветка.
- Общая сцена, prefab, карта, настройки проекта или другой конфликтный ресурс
  одновременно имеют одного владельца.
- Остальные участники передают владельцу готовый компонент, отдельный ассет или
  точную инструкцию интеграции.

## Офисный roadmap

- `OFFICE_ROADMAP.md` — обязательный живой координационный контракт для любой
  работы с офисным эпизодом: сценой, кодом, контентом, интеграциями, тестами или
  документацией.
- Исполнитель читает roadmap до назначенного task-файла и редактирует его в той же
  feature-ветке перед `Handoff`: обновляет milestone, проверенные факты, блокеры,
  следующий срез и историю изменений.
- Офисная задача не получает статус `Done`, пока roadmap не актуализирован.
- `DEVELOPMENT_SPEC.md`, решения продюсера и общие технические контракты имеют
  приоритет над roadmap. После изменения источника правды roadmap обновляется в
  той же задаче.

## Изменения интерфейсов

- Публичный интерфейс между системами сначала записывается здесь.
- Переименование общей сцены, события, input action или публичного API требует
  согласования с затронутыми владельцами.
- Изменение контракта и его документации попадает в один commit.

## Unity YAML и Project Settings

- Сериализованные Unity YAML-файлы не проходят внешнее удаление trailing spaces:
  пустые поля вида `m_Name: ` являются каноническим выводом Unity 6000.5.6f1.
- `.editorconfig` сохраняет такой вывод, а атрибут `unity-yaml` отключает для него
  ошибку `blank-at-eol` в Git-проверках.
- `ProjectSettings/ProjectAuditorSettings.asset` остаётся отслеживаемой проектной
  настройкой. Одноразовая нормализация `m_Name:` в `m_Name: ` не меняет значение и
  принимается как baseline; её нельзя циклически откатывать при открытом Editor.
- Любой другой diff в `ProjectSettings/**` проверяется по содержанию и не считается
  шумовым автоматически.

## Старт приложения и сохранение

- `Assets/Game/Scenes/Main.unity` — сцена с build index `0` и единственная точка
  входа в игру.
- Состав Build Settings: `Main` (0), `CharacterSelect` (1), `SampleScene` (2),
  `Prologue_Photo` (3), `Prologue_Office` (4), `HotelArrival` (5). Изменение
  порядка согласуется с интегратором, потому что «Продолжить» и общий flow
  проверяют наличие сцены в Build Settings.
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

## Результат эпизода и общий flow

- `EpisodeResult` в `Assets/Game/Core/Flow/` — единственная форма, в которой эпизод
  сообщает свой результат общему flow: `characterId`, имя собственной сцены,
  `checkpointId`, episode-owned `payloadJson`, признак завершения эпизода, ключи
  текста прибытия и список читаемых строк итога `EpisodeResultLine`. Core не
  интерпретирует payload и не знает, что означают строки.
- `GameFlowService.CompleteEpisode(EpisodeResult)` сохраняет checkpoint эпизода и
  открывает сцену прибытия. `GameFlowService.FinishArrival(result)` возвращает
  checkpoint на сцену самого эпизода, вызывает `GameSaveService.LeaveCharacterLine`
  и открывает `CharacterSelect`.
- `GameFlowService.FinishArrivalToMainMenu(result)` выполняет ту же финализацию,
  но открывает `Main`. Сохранённой Continue-сценой остаётся `CharacterSelect`,
  потому что runtime-only `PendingResult` нельзя восстановить после перезапуска.
- Эпизод не загружает напрямую ни сцену другого эпизода, ни `HotelArrival`, ни
  главное меню. Он отдаёт один `EpisodeResult` и не знает имени сцены прибытия.
- `PendingResult` переносит результат через загрузку сцены; `IsSceneInBuild`
  защищает оба перехода. Если `HotelArrival` отсутствует в Build Settings, flow
  безопасно деградирует до `CharacterSelect`.
- `Assets/Game/Scenes/HotelArrival.unity` — общая параметризованная сцена прибытия
  для всех трёх героев. `HotelArrivalController` собирает UI в коде по данным
  `EpisodeResult`, откатывается к персональным строкам таблицы `Common` и даёт два
  явных выхода: в `CharacterSelect` и в `Main`. Сцена пересобирается пунктом меню
  `Jam/Flow/Rebuild Hotel Arrival`.
- `GameEntryPoint` записывает как последнюю сцену линии любую загруженную сцену,
  поэтому переход через общую сцену прибытия обязан вернуть в checkpoint имя сцены
  самого эпизода. Без этого повторный выбор героя открывает экран прибытия вместо
  забега.
- Офисные checkpoint ID: `office.setup`, `office.run`, `office.arrival`.
  Пробуждение не является границей сохранения. Payload
  (`OfficeCharacterSaveData`, схема `1`) принадлежит эпизоду, сериализуется только
  `OfficeCheckpointAdapter` и остаётся непрозрачным для Core.
- `OfficeStoryDirector` владеет фазами `Setup → Run → Awakening → Arrival` и
  предоставляет `IGameModeSaveProvider` только пока забег действительно идёт.

## Глобальный HUD и ручное сохранение

- Persistent `GameEntryPoint` создаёт `GlobalHudController` при запуске любой
  сцены; отдельные игровые сцены не создают собственное pause-меню.
- HUD скрыт в `Main`, `CharacterSelect` и `HotelArrival`. В гостинице навигацией
  владеет `HotelArrivalController`; в остальных игровых сценах HUD показывает
  кнопку `МЕНЮ [ESC]` и overlay: `Продолжить`, `Сохранить`, `Выйти в главное меню`.
- Открытый overlay ставит `Time.timeScale = 0`, освобождает курсор и обязан
  восстановить прежние значения при закрытии или смене сцены.
- Режим, поддерживающий ручное сохранение, предоставляет ровно один активный
  `IGameModeSaveProvider` в своей сцене. Core не знает структуру payload режима.
- `GameModeSaveService` находит provider только в активной сцене, вызывает его
  `TrySave`, затем делает `GameSaveService.Flush`.
- Выход в `Main` не выполняет неявное сохранение. `GameEntryPoint` не записывает
  `Main` как Continue-сцену, поэтому «Продолжить» возвращает к последнему явно
  сохранённому checkpoint.
- Photo реализует provider через `PhotoCheckpointAdapter`; допустимы checkpoint
  `photo.intro`, `photo.explore`, `photo.camera`, `photo.published`, `photo.arrival`.
- `FinaleUnlocked` становится истинным только после завершения всех трёх линий.
- `CharacterSelect` имеет build index `1`; отсутствующая эпизодная сцена линии
  Drive временно заменяется `SampleScene`, но выбранный `CharacterId` всё равно
  сохраняется.

## Катсцены

- `CutsceneDirector` — единственный runtime-оркестратор катсцен и создаётся
  persistent `GameEntryPoint` вместе с глобальным HUD.
- Сцена предоставляет presentation-компонент с уникальным стабильным
  `CutsceneId`: `UiStoryboardPresentation` или `TimelineCutscenePresentation`.
- `ICutscenePresentation` отвечает только за показ, пропуск и остановку;
  сюжетные условия и gameplay-state остаются в NodeCanvas/контроллере эпизода.
- `PlayCutsceneTask` запускает катсцену по ID и завершает NodeCanvas action после
  `Completed` или `Skipped`. Следующая task меняет авторитетный state и сохраняет
  checkpoint — Timeline/Storyboard не записывают прогресс самостоятельно.
- Во время катсцены глобальная кнопка меню скрыта. `Escape` принадлежит
  `CutsceneDirector` и пропускает сцену, только если presentation это разрешает.
- Смена сцены или остановка graph завершает текущую катсцену с неуспешным
  результатом; callback применяется не более одного раза.
- Середина Timeline и номер storyboard-кадра не входят в save payload: загрузка
  возвращает игрока к устойчивой границе до или после короткой катсцены.
- Стабильный ID вступления героини — `photo.prologue.intro`; границы сохранения:
  до показа — `photo.intro`, после `Completed` или `Skipped` — `photo.explore`.
- `Prologue_Photo` содержит ровно один `UiStoryboardPresentation` для этого ID;
  его данные принадлежат `PhotoIntroStoryboard.asset`, а контроллер только
  применяет результат к FSM и checkpoint.

## Ввод

- Вся игра, включая gameplay, меню и UI, использует только Unity New Input System.
- Единственный общий runtime-asset ввода — `InputSystem_Actions.inputactions`; его
  владельцем является интегратор.
- Эпизоды используют согласованные action maps и запрашивают новые actions у
  интегратора вместо создания параллельных assets.
- Карта `Office` содержит `Move`, `Aim`, `Primary`, `Secondary` и `Interact` для
  связок «клавиатура + мышь» и геймпада. Офисный эпизод читает `Office/Move` и
  `Office/Primary`; временные `Player/Move` и `Player/Attack` в офисе больше не
  используются.
- Legacy Input Manager, `UnityEngine.Input` и `StandaloneInputModule` запрещены.
- UI работает через `InputSystemUIInputModule`; gameplay — через `InputAction`,
  `PlayerInput` или общий адаптер ввода.
- Игровой код не привязывается напрямую к конкретной клавиатуре, мыши или геймпаду.

## Локализация и текст

- Единственный runtime API — `Jam.Core.Localization.Loc`; прямое чтение String
  Tables игровыми системами не допускается.
- Язык хранится в `jam.settings.locale` отдельно от `jam.save.v1`; новая игра не
  сбрасывает пользовательский язык.
- Общий UI принадлежит таблице `Common`, episode-local строки — таблице эпизода.
- Новый UI использует TextMeshPro. `UnityEngine.UI.Text` и legacy `TextMesh`
  запрещены; fallback-строка обязательна для критического UI.
- Статические TMP-элементы используют `LocalizedTextBinding`, динамические
  подписываются на `Loc.LocaleChanged` и пересобирают видимое состояние.
- Полный процесс и naming находятся в `localization/README.md` и `KEY_NAMING.md`.

## Звук

- Persistent `GameEntryPoint` создаёт ровно один `Jam.Core.Audio.AudioService`;
  эпизоды не создают собственный глобальный mixer, музыкальный контроллер или
  параллельный каталог пользовательских громкостей.
- Общие логические шины: `Master`, `Music`, `Sfx`, `UI`, `Ambience`, `Voice`.
  Значения `0..1` хранятся в `PlayerPrefs` под `jam.settings.audio.<bus>` отдельно
  от `jam.save.v1`; новая игра их не сбрасывает.
- Контент запускается через project-owned `AudioCue`: стабильный ID, варианты
  клипа, шина, volume/pitch, spatial blend, loop, cooldown, concurrency и priority.
- Основной runtime API — `IAudioService`/`AudioService`: `Play`, `Stop`,
  `PlayMusic`, `StopMusic`, `PlayVoice`, `StopVoice`, громкости и mix-context.
  Эпизодный код не обращается к persistent `AudioSource` напрямую.
- Контексты имеют фиксированный приоритет `Paused > Cutscene > Default` и
  регистрируются по владельцу. Каждый владелец обязан очистить свой контекст при
  завершении/disable; закрытие pause восстанавливает активный cutscene-контекст.
- `AudioConfiguration` по пути Resources `Audio/AudioConfiguration` опционально
  связывает логические шины с `AudioMixerGroup` и snapshot. Без ассета действует
  runtime fallback через громкости источников, поэтому базовый flow не блокируется.
- Storyboard voice маршрутизируется через `AudioService`; NodeCanvas использует
  только project tasks `PlayAudioCue`, `SetMusic`, `SetAudioContext` и не хранит
  аудиосостояние в Blackboard/save.

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
