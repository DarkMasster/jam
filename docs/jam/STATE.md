# Текущее состояние

Последнее обновление: 2026-08-02
Ответственный за обновление: интегратор
Последний проверенный commit проекта: f4e41c4 (сборка не проверена)
Последний проверенный билд: отсутствует
Последняя проверенная веб-демка: [office](../../web-demos/office/) — 2026-08-01

## Photo PolygonOffice room vertical slice

- `Prologue_Photo` contains a project-owned `PhotoRoomDiorama` wrapper built from read-only PolygonOffice prefabs.
- `RoomSecret`, `RoomPhoto` and `MotherDialogue` use separate authored Cinemachine cameras rendered into the existing TMP stage through a runtime RenderTexture.
- Cinemachine 3.1.7 owns three room virtual cameras and one portrait virtual camera. Two channel-isolated CinemachineBrains drive the room and portrait RenderTexture output cameras.
- Existing `PhotoPrologueRules`, checkpoints, AudioCue and NodeCanvas Dialogue Trees remain authoritative and unchanged.
- No characters are rendered in the 3D room diorama. A separate off-stage 3D portrait rig renders a head-and-shoulders view of the current speaker on a dark background at the left of the lower dialogue panel.
- Heroine and mother have separate Cinemachine portrait cameras and warm/cool key-fill lighting setups, allowing independent framing without moving the room camera or sharing actor transforms.
- Project-owned `PhotoPortraitPose` applies lightweight static humanoid poses in `LateUpdate`, removing the visible T-pose without adding Animator Controllers. Room camera transitions use a 0.2-second Cinemachine EaseInOut; portrait speaker changes remain cuts.
- A second project-owned entrance diorama now covers `MailboxHunt`, `MailboxPublication` and `MailboxReaction`, using custom mailbox, marked summons and butterfly props plus three dedicated Cinemachine shots.
- A third project-owned airport diorama covers `AirportPhoto`, `BorderControl` and `Summary`: terminal seating and board, glass passport booth, stamped passport, three Cinemachine shots and a separate security-officer portrait RenderTexture. No characters are placed inside the stage diorama.
- A manual UI smoke run passed the complete Photo Prologue route from `Main` and returned to `CharacterSelect`. A separate pause-menu save at `photo.explore` restored the exact `RoomPhoto` step through `Main -> Continue`.
- Photo smoke testing still logs intermittent `referenced script (Unknown) ... missing` during scene transitions despite no null component in a live loaded-scene scan; URP also reports additional-light shadow-atlas downscaling. Neither issue blocked the tested route, but both need a focused cleanup pass before final build.
- `Jam/Photo/Create Polygon Room Diorama` deterministically rebuilds the wrapper prefab and scene instance without modifying `Assets/PolygonOffice/**`.
- Unity 6000.5.3f1 compiled without errors; scene validation reported 0 missing scripts and 0 broken prefabs. Final portrait framing and poses are still pending.

## Сейчас

- Выбор первого персонажа открывает автономную HTML-игру Drive из
  `StreamingAssets/Web/Drive/hasta-la-vista-jam.html` в системном браузере.
  Активная линия сохраняется в Unity, а при отсутствии файла используется
  fallback `Prologue_Drive`; обратный сигнал о завершении из HTML пока отсутствует.

- Unity 6000.5.3f1 проект существует и использует URP 17.5.0.
- Для всей игры утверждён Unity New Input System; фактически установлен пакет
  1.20.0, legacy Input Manager не используется.
- Установлены и загружаются без ошибок NodeCanvas 3.42 и Damage Numbers Pro 4.55; паттерны использования зафиксированы в `INTEGRATIONS.md`.
- Подключены Unity Localization 1.5.12, Addressables и TextMeshPro; `ru`, `en` и
  `qps-ploc` используют таблицы `Common`, `Photo`, `Office`.
- Реализован общий persistent-аудиофундамент: шесть логических шин, cue-ассеты,
  pool SFX, crossfade музыки, voice-канал, пользовательские громкости и контексты
  `Paused > Cutscene > Default`.
- Командный пайплайн развёрнут в docs/jam/.
- Зафиксированы тема `Reflection + Momentum`, свободный порядок героев, цель
  Пролога и структура всех трёх актов.
- Основной фокус команды — сценарий и вертикальный срез Пролога.
- Полная игра рассчитана на 3–4 часа; джем-билд ограничен Прологом на 15–30 минут.
- Определены три дорожных события героя 1, цель офисного сна героя 2 и центральный
  фотоконфликт героини 3.
- Архитектура фотопролога актуализирована: NodeCanvas FSM/Dialogue Trees управляют
  режиссурой, C# — фотомеханикой, `GameSaveService` — четырьмя смысловыми checkpoint,
  а Damage Numbers Pro — только визуальной обратной связью.
- Реализован сквозной white-box фотопролога: `Main -> CharacterSelect ->
  Prologue_Photo`, экспозиция, три точки осмотра, камера, выбор повестки/бабочки,
  публикация, рефлексия и транзитная гостиница.
- На ветке `feature/photo-prologue` начат production-срез из трёх сцен: Комната,
  Подъезд и Аэропорт. Работают шкалы 20/20, выборы комнаты, три публикации,
  `ProloguePath`, пропускаемый аэропортовый снимок и паспортный контроль.
- Числа шкал Photo показываются только в обучении. После паспорта запускается
  storyboard «Продолжение следует», затем flow возвращает в `CharacterSelect`.
- Проверен финальный Balance-путь Photo без аэропортового снимка: outro запускается,
  Skip возвращает в выбор персонажей, checkpoint `photo.arrival` содержит
  завершённый payload, Unity Console не содержит ошибок и предупреждений.
- Проверены новая игра и Continue для Photo: новый запуск начинает `RoomSecret` с
  `20/20`, а `photo.explore`, `photo.camera` и `photo.published` восстанавливают
  соответственно Комнату, Подъезд и Аэропорт. Тест не изменяет пользовательский save.
- Photo presentation приведён к композициям мокапов: диалог с портретной зоной,
  видоискатель с сеткой, трёхкарточная матрица публикации и компактный итог. Фоны и
  портреты пока являются заглушками; state, checkpoint и обработчики не изменены.
- Photo payload обновлён до schema `3`; v1/v2 мигрируют, а стабильные checkpoint ID
  и общий Core API не изменились.
- Создана единая спецификация разработки `DEVELOPMENT_SPEC.md` с архитектурой,
  критериями готовности и разделением владения.
- Реализована стартовая сцена `Main`: entry point, главное меню и базовое локальное
  сохранение для команд «Новая игра» и «Продолжить».
- Главное меню получило DarkUI art-pass: двухколоночную композицию, DarkUI-кнопки,
  иконки и разделитель. Локализация, сохранения и навигационные контракты не менялись.
- `CharacterSelect` использует DarkUI-карточки и три одновременно отображаемых
  RenderTexture-портрета из PolygonOffice-моделей; камеры портретов управляются
  изолированными Cinemachine channels. Общий pause-menu также использует DarkUI,
  при этом episode-owned UI офиса не изменялся.
- Реализована сцена `CharacterSelect` со свободным выбором трёх линий, отдельными
  checkpoint персонажей и индикатором прогресса `0/3`.
- Создана запускаемая `Prologue_Drive`: четырёхкадровая московская экспозиция
  использует PolygonOffice-диораму, отдельные Cinemachine-планы и RenderTexture-
  портрет героя. Завершение или skip сохраняет `drive.departure` и возвращает в
  `CharacterSelect`; дорожный gameplay будет подключён после этой границы.
- Все существующие storyboard-катсцены героев 2 и 3 используют scene-local 3D
  presentation: Office Setup/Awakening получили отдельные диорамы и портрет
  разработчика, Photo Intro/Outro переиспользуют комнату, аэропорт и портретный
  риг героини. Стабильные cutscene ID и save-flow не изменены.
- Office gameplay HUD приведён к стилю DarkUI главного меню: objective, coach,
  Momentum, status и failure overlay используют общие тёмные округлые панели.
  Красный сохранён как семантика угрозы; bindings и gameplay-state не менялись.
- Runtime-интерфейс Photo использует общий `DarkUiTheme`: рамки story/stage,
  нижняя диалоговая панель, кнопки и карточки получили DarkUI-спрайты. Мятный и
  розовый сохранены как семантика Честности/Признания; portrait RenderTexture
  остаётся слева и принудительно выводится последним sibling поверх панели.
- В `web-demos/office/` существует независимый Three.js-прототип `Offboarding`,
  проверяющий core loop офисного эпизода до переноса выбранных механик в Unity.
- В `DEVELOPMENT_SPEC.md` зафиксирована рабочая палитра офисного эпизода с HEX/RGB,
  пропорциями и правилами применения для окружения, эффектов и UI.
- Офис задуман огромным, но джем-билд использует один короткий авторский маршрут;
  процедурная генерация рассматривается только как post-jam эксперимент.
- В целевой Unity-версии офисного эпизода предметы подбираются автоматически;
  `Interact` не используется для подбора во время быстрого боя.
- В офисном сне нет проходимого выхода: после сбора ноутбука и кружки у ложной
  двери `EXIT` катсцена собирает босса из множества серверных стоек. Босс в любом
  случае побеждает героя; этот постановочный проигрыш завершает эпизод и будит
  героя, тогда как обычное раннее поражение ведёт к restart.
- Серверный босс говорит механическим жужжанием и щелчками дисков. Его разные
  оскорбления про увольнение и замену героя искусственным интеллектом показываются
  синхронными голографическими проекциями над корпусом.
- Финальный бой состоит из двух стадий: единый корпус распадается на отдельные
  серверные стойки, они окружают героя кольцом, ненадолго возвращают ему управление
  и затем синхронно наносят неизбежный сюжетный удар. Герой погибает внутри сна и
  просыпается в машине.
- На ветке `feature/office-unity-scene` создан первый запускаемый Unity-срез
  `Prologue_Office`: пять зон офисного маршрута, top-down камера и движение,
  автоматический сбор ноутбука и кружки, HUD и физически закрытая дверь `EXIT`.
- Первый офисный срез временно использует существующие `Player/Move` и
  `Player/Attack` из общего input asset. Карта `Office` ещё не создана
  интегратором; сам asset в задачах не изменялся.
- На той же ветке выполнен срез `M1A`: в офисе работает первый action-loop —
  клавиатура автоматически выбирается перед героем с обводкой цели и берётся
  свободными руками, занятые руки не заменяют предмет, `Primary` бросает его с
  коротким lockout повторного подбора, а брошенный предмет переводит принтер из
  `Intact` в `Broken` со вспышкой и счётчиком разрушений в HUD.
- Срез `M1B` добавил ожившие офисные кресла с красным телеграфом и рывком,
  базовый Momentum, три деления работоспособности и мягкий restart за 1,1 с.
- На ветке `feature/office-m2-route` завершён `M2`: одна контекстная строка HUD
  последовательно объясняет движение, автоподбор, бросок, телеграф и цель; у
  переговорной появляется задержанное Reflection-эхо, а пропущенные ноутбук и
  кружка переносятся к `EXIT` и физически блокируют дальнейший путь до сбора.
- На ветке `feature/office-m3-boss` завершён `M3`: 12 серверных стоек у ложного
  `EXIT` собираются в единый корпус, принимают три броска, перестраиваются в
  физически замкнутое кольцо и проводят неизбежный сюжетный удар. Раннее поражение
  по-прежнему перезапускает маршрут, а финальный удар фиксирует episode-local
  завершение без restart.
- На ветке `feature/office-m4-flow` завершён `M4`: в Core появился общий слой flow
  `Assets/Game/Core/Flow/` — `EpisodeResult` с checkpoint эпизода, непрозрачным
  payload, ключами текста прибытия и читаемыми строками итога, `GameFlowService` и
  параметризованная сцена `HotelArrival`, единая для всех трёх героев. Эпизод
  больше не загружает чужую сцену или меню сам, а отдаёт один результат.
- На той же ветке офис получил `OfficeStoryDirector` с фазами
  `Setup → Run → Awakening → Arrival`, пропускаемые сториборды
  `office.prologue.setup` и `office.prologue.awakening` через общий
  `CutsceneDirector`, checkpoint `office.setup`, `office.run`, `office.arrival` с
  episode-owned payload схемы `1` и ручное сохранение через `IGameModeSaveProvider`.
- В том же срезе закрыты зависимости интегратора: в общий
  `InputSystem_Actions.inputactions` добавлена карта `Office`
  (Move, Aim, Primary, Secondary, Interact), офис перешёл с временных
  `Player/Move` и `Player/Attack` на `Office/Move` и `Office/Primary`, а Build
  Settings содержат `Main` (0), `CharacterSelect` (1), `SampleScene` (2),
  `Prologue_Photo` (3), `Prologue_Office` (4) и `HotelArrival` (5).
- `CharacterSelect` благодаря этому загружает для офисной линии `Prologue_Office`
  вместо `SampleScene`.
- Создан живой `OFFICE_ROADMAP.md`: он фиксирует milestones, зависимости,
  сокращения и следующий офисный срез. Для любой работы с офисом его чтение и
  редактирование перед `Handoff` обязательны.
- Исправлен `LiberationSans SDF - Fallback.asset`: в `main` лежал выросший в Play
  Mode динамический атлас (16 записей `m_AtlasTextures` при 13 текстурах), из-за
  чего мировые TMP-надписи падали с `UnassignedReferenceException`. Ассет сброшен к
  чистому состоянию, осиротевшие sub-asset текстуры удалены, правило хранения
  зафиксировано в `DECISIONS.md` и `CONTRACTS.md`.
- Устранена причина повторного whitespace-diff в
  `ProjectSettings/ProjectAuditorSettings.asset`: канонический Unity YAML сохранён,
  editor formatting и Git whitespace-check настроены не удалять значимый для
  сериализатора завершающий пробел пустого поля.

## Работает

- Unity-проект и исходный Git-репозиторий.
- Долговременная память команды в `docs/jam/`.
- Проверенные интеграционные правила NodeCanvas, GameSaveService и Damage Numbers Pro.
- Регламент feature-веток и интеграции в `main`.
- `Main` запускается первой, создаёт UI и Input System EventSystem; доступны
  «Новая игра», «Продолжить» и «Выход».
- «Новая игра» открывает `CharacterSelect`; выбор линии Drive временно ведёт в
  `SampleScene`, пока её эпизодная сцена отсутствует.
- «Продолжить» восстанавливает последнюю сцену и активного героя после остановки
  игровой сессии.
- Эпилог в `CharacterSelect` блокируется до завершения Office и Photo; Drive не
  участвует в условии. При завершении второй из требуемых линий браузерный эпилог
  открывается автоматически, повторно его можно открыть кнопкой меню.
- Выбор персонажа 3 загружает `Prologue_Photo` с build index `3` без fallback;
  `photo.explore`, `photo.camera`, `photo.published` и `photo.arrival` сохраняют
  versioned payload, а «Продолжить» восстанавливает устойчивую фазу.
- Photo payload переведён на схему `2`: единая модель линии содержит три акта,
  legacy payload `version=1` мигрирует при чтении, а повреждённые состояния
  откатываются к последней допустимой фазе.
- Завершение Photo production-среза сохраняет `prologue.completed`, отмечает Photo
  завершённой для эпилога и возвращает игрока в выбор персонажа.
- Общий persistent HUD доступен во всех игровых сценах: `МЕНЮ [ESC]` открывает
  pause-overlay с продолжением, ручным сохранением режима и выходом в `Main`.
- Ручное сохранение маршрутизируется через `IGameModeSaveProvider`;
  Photo сохраняет текущую фазу, включая позицию внутри вступительного диалога.
- `Prologue_Office` запускается напрямую в Play Mode: игрок проходит стартовый
  кабинет, open space, стеклянную переговорную, серверную и рецепцию; оба личных
  предмета собираются автоматически, а `EXIT` остаётся ложной закрытой целью.
- В том же прогоне работает петля «подбор → бросок → разрушение»: по маршруту
  расставлены восемь клавиатур и четыре принтера, HUD показывает состояние рук и
  счётчик разрушенной техники.
- Офисный маршрут работает как цельный `M0–M3`-срез: кресла создают давление,
  Momentum реагирует на движение и разрушение, подсказки сменяются по фактическим
  действиям, Reflection beat восстанавливается после restart, а fallback личных
  вещей переносит и сами pickups, и их напольные маркеры перед закрытой зоной.
  Босс собирается из 12 стоек, принимает три броска, замыкает героя в кольце и
  завершает забег только общим финальным ударом; для боя внутри арены доступны три
  дополнительные клавиатуры.
- Полный путь `Main → CharacterSelect → Prologue_Office → HotelArrival →
  CharacterSelect` проходится в Play Mode: Setup играет и пропускается, забег
  сохраняется в `office.run`, финальный удар ведёт к пробуждению и экрану прибытия
  с retries, ноутбуком, кружкой и разрушенной техникой, а возврат восстанавливает
  checkpoint офиса на `Prologue_Office`, поэтому повторный выбор героя снова
  начинает забег с `Setup`.
- Проверен отдельный путь возврата: из Office общий HUD открывает `Main` и
  сохраняет Continue на `Prologue_Office`; из `HotelArrival` две явные кнопки
  ведут в `CharacterSelect` или `Main`, причём Continue после Main указывает на
  `CharacterSelect`, а runtime-only результат прибытия очищается.
- Ручное сохранение работает и в офисе: общий pause-overlay сохраняет забег через
  `OfficeStoryDirector`, но только пока забег действительно идёт.
- Базовая гибридная система катсцен включает persistent `CutsceneDirector`,
  UI-сториборды, Timeline-адаптер и ожидающую NodeCanvas task по стабильному ID.
- `GameEntryPoint` создаёт один `AudioService`; pause и cutscene согласованно
  приглушают микс, storyboard voice использует общий Voice-канал, а NodeCanvas
  получает тонкие tasks для cue, музыки и именованного mix-context.
- Вступление Photo подключено как реальный UI-сториборд `photo.prologue.intro`:
  четыре кадра запускаются из общего Director, а `Completed` и `Skipped`
  одинаково переводят FSM в `Explore` и сохраняют `photo.explore`.
- Главное меню переключает русский и английский без перезапуска. Общий UI,
  CharacterSelect, storyboard, Photo white-box и Office HUD/state используют TMP
  и локализованные ключи; проверенные runtime-сцены содержат `legacy Text = 0`.
- Веб-демо [office](../../web-demos/office/) запускается через локальный статический
  сервер: проверены стартовая сцена, переход в забег, HUD и restart клавишей `R`.
- Индекс `ccc` обновлён и успешно обрабатывает все 634 индексируемых файла
  проекта без ошибок.
- В `Assets/PolygonOffice` добавлен read-only snapshot Synty POLYGON Office из
  `eyetengu/2024_March_Office` commit `6034246`; исходные `.meta` сохранены, сцены
  и настройки вне vendor-каталога, а также несвязанные наборы не импортированы.
  Unity 6000.5.6f1 завершил refresh, распознал 808 prefab'ов и не показал ошибок;
  семь предупреждений относятся только к смене порта MCP после domain reload.
- Для будущего art-pass выбран безопасный паттерн: текущие Office-prefab'ы и
  greybox-коллайдеры сохраняют gameplay, а модели POLYGON Office подключаются как
  visual children. Маршрут, проходы, line of sight и арена босса не меняются.
- На ветке `polish/office-synty-art-pass` этот паттерн реализован и проверен:
  editor-слой `OfficeArtPass` подключает модели пака как visual children,
  подгоняет каждую под явно указанный greybox-объём, выключает vendor-коллайдеры и
  переводит материалы пака в офисную палитру project-owned material variants
  `M_Synty_*.mat`. Слайс `A1` заменил стартовый кабинет, пару столов и кресел open
  space на `z = -15`, левую секцию стекла переговорной и две серверные стойки.
  Слайс `A2` закрыл open space целиком: оставшиеся 4 стола и 4 кресла подов
  `z = -20.5` и `z = -9.5`, 12 фоновых столов и 12 колонн `BackgroundScale`.
  Затем перенесена вся оставшаяся статичная мебель: 10 серверных стоек, 12 стоек
  босса, стол и 4 кресла переговорной, 2 стойки рецепции. Коллайдеры, маршрут,
  триггеры и `Assets/PolygonOffice` не изменились — greybox-коллайдеры остались
  `171` из `171` включённых.
- Vendor-коллайдеры art-pass не выключает, а удаляет. Выключенный коллайдер
  остаётся в иерархии владельца, и `OfficeBossEncounter.SetRackColliders(true)`
  включил бы vendor-коллайдер стойки прямо во время боя, потому что собирает их
  через `GetComponentsInChildren`.
- Подложка `Void Floor` расширена с `42 × 76` до `400 × 400`: горизонт больше не
  обрывается краем этажа. Коллайдера у неё нет, границу игровой зоны по-прежнему
  держат `Playable Floor` и стены.
- На ветке `polish/office-dnp-feedback-runtime` завершён M7: project-owned
  DNP-адаптер и три семантических preset подключены к подтверждённым событиям
  урона, разрушения и подбора. Исправлена несовместимость demo TMP-материала с
  URP и ориентация world-space текста; popup подбора больше не выглядит как
  фиолетовый квадрат. Controlled Play Mode подтвердил урон по креслу, боссу и
  герою, разрушение принтера, подбор клавиатуры/ноутбука/кружки, milestone обеих
  личных вещей, отрицательные ветки и сохранность HUD/SFX/частиц/дрожи. Быстрые
  popup над одной целью теперь занимают четыре вертикальные полосы; текстовые
  уведомления уменьшены и не объединяются, поэтому pickup и milestone остаются
  раздельными. Итоговый вид подтверждён пользователем.
- На ветке `feature/office-m8-destructibility` завершён M8: к четырём принтерам
  добавлены 32 разрушаемых объекта (мониторы, столы, шкафы, рецепция, серверные
  стойки и стекло). Все `36` используют подготовленные `Intact → Broken`,
  per-object Momentum и существующий DNP feedback; HUD считает только `16`
  принтеров и стоек. Broken-состояния отключают собственные коллайдеры и становятся
  проходимыми, включая утверждённый продюсером новый проход через стекло.
- Герой теперь ломает окружение не только броском: накопленная planar velocity
  передаётся в impact-контракт по формуле веб-демо
  `speed × (1 + Momentum × 0.4)`. Базовый разгон ломает лёгкие объекты, а стойка
  требует усиления Momentum. Restart восстанавливает все `64` коллайдера M8.

## Частично работает

- Сюжетная концепция описана на уровне исходных вводных, но ещё не готова для
  написания диалогов и cutscenes.
- Исследование, камера и публикация Photo пока собираются программно внутри
  `PhotoWhiteboxController`; production Dialogue Trees, графические hotspots,
  фоновые иллюстрации, портреты и DNP-feedback ещё не созданы.
- Production-логика трёх сцен уже проходит до финального storyboard, но layout пока
  программный white-box без финального арта.
- В Photo подключены production NodeCanvas Dialogue Trees разговора с матерью и
  паспортного контроля. Их subtitle/choice events отображаются существующим TMP UI,
  а последствия по-прежнему применяются только через `PhotoPrologueRules`.
- Для Photo созданы project-owned AudioCue комнаты/дождя, аэропорта, затвора,
  двери и паспортного штампа. Сейчас они используют локально сгенерированные
  прототипные WAV, которые требуется заменить финальным саунд-дизайном.
- Оба production Dialogue Tree Photo вручную пройдены из `Main` по доступным
  веткам; продюсер подтвердил успешный результат.
- Веб-демо не связано с `Assets/` и не является Unity-сборкой. Его Three.js-модули
  загружаются локально, а Three.js и bloom при первом запуске запрашиваются с unpkg.
- Сцена `HotelArrival` работает, но остаётся текстовой заглушкой: она читает
  `EpisodeResult` и показывает строки итога, без арта и звука.
- Art-pass POLYGON Office закрыл всю статичную мебель маршрута; greybox остаются
  оболочка этажа (пол, стены, потолок), стекло переговорной, порог рецепции и
  `EXIT`, а также интерактивные предметы и противники. Отдельно не решён вопрос
  `Reflection Panel`: рядом с переплётами модуля пака он читается как светящийся
  щит, а не как отражение в стекле.
- Офисные DNP popup зависают: цифра `−1` остаётся висеть при уничтожении противника
  и над героем вместо того, чтобы доиграть подъём и погаснуть. Причина разобрана в
  `OFFICE_ROADMAP.md` («Открытый дефект `M7`»): адаптер передаёт `Transform` во все
  ветки `Spawn`, из-за чего DNP включает следование на pooled-экземпляре и больше
  никогда его не сбрасывает, а следование за уничтоженной целью перестаёт двигать
  popup. Игровое состояние не затронуто — дефект только presentation.
- Офисный путь проверен только в Play Mode: Windows-сборка ещё не собиралась, а
  длительность цикла 5–10 минут ещё не замерена.
- `GameSaveService.LeaveCharacterLine` не выставляет `CompletedCharacters`, поэтому
  счётчик `0/3` в `CharacterSelect` по-прежнему растёт только после завершения
  полной линии — то же поведение, что у Photo.
- Production-аудиоконтент, `AudioMixer`/snapshot asset и экран настроек громкости
  ещё не созданы. Runtime уже работает без конфигурационного ассета и сохраняет
  значения через публичный API.

## Сломано или заблокировано

- Windows x64 build недоступен на текущей машине: в Unity 6000.5.6f1 установлены
  только `MacStandaloneSupport` и `WebGLSupport`. Требуется доустановить
  `Windows Build Support (Mono)` через Unity Hub.
- Пробная сборка `StandaloneOSX` не завершилась: после запуска редактор перестал
  отвечать (`Command TCS timed out`), каталог `Builds/` не создан. Похоже на
  модальное окно; перед следующей сборкой Unity нужно открыть и закрыть его вручную.

- Нельзя писать полный сценарий до ответов на вопросы P0 из `QUESTIONS.md`.
- Текущий объём: три героя × три акта × три разных мини-игры — не помещается в
  48 часов без общего каркаса и жёсткого сокращения контента.
- Unity-изменения и нормализация Git LFS сохранены в `feature/current-unity-work` и ещё не влиты в `main`.
- Полный перенос меню и стены увольнения из веб-демо, механика сигарет и новый
  набор противников требуют отдельных решений перед переносом в Unity. Мотив
  замены героя искусственным интеллектом утверждён только для реплик финального
  босса.

## Следующий шаг

Добавить иллюстрации и финальный звук в `PhotoIntroStoryboard`, заменить
прототипные Photo WAV на production-записи и вынести оставшиеся временные экраны
из `PhotoWhiteboxController` в компоненты `PhotoEpisodeController` при сохранении
работающих Dialogue Trees, FSM, checkpoint ID и маршрута из главного меню.
Следующий офисный срез art-pass закрывает оболочку этажа — стекло переговорной,
порог рецепции, `EXIT`, пол, стены и потолок, — а последним идёт интерактив с
противниками и повторной проверкой полного run/restart/boss flow.
Отдельный офисный срез `M5` собирает Windows x64 build, проходит в нём полный путь
`Main → CharacterSelect → Prologue_Office → HotelArrival`, замеряет длительность
цикла и подгоняет её к 5–10 минутам без soft lock, после чего записывает результат
smoke test. Затем идёт полировка `M6`: SFX, попадания, частицы, дрожь камеры и свет
по Momentum.

## Следующая контрольная точка

Утвердить состав механик офисного MVP и получить первый играбельный Unity-прототип
не позднее 12-го часа; веб-демо используется как референс game feel.
