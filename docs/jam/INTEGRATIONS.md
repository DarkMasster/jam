# Паттерны сторонних интеграций

Последняя проверка: 2026-08-02
Среда проверки: Unity 6000.5.6f1, загруженные сборки Editor, Console без ошибок и предупреждений.

Этот документ — долговременная память о том, как команда использует купленные ассеты. Он фиксирует границы ответственности, чтобы эпизоды можно было делать параллельно и не связывать игровую логику с vendor-кодом.

## Установленные версии

| Инструмент | Версия | Vendor-каталог | Сборка |
|---|---:|---|---|
| NodeCanvas | 3.42 | `Assets/ParadoxNotion/NodeCanvas`, `Assets/ParadoxNotion/CanvasCore` | `NodeCanvas`, `ParadoxNotion` |
| Damage Numbers Pro | 4.55 | `Assets/DamageNumbersPro` | `DamageNumbersPro` |
| Synty POLYGON Office | source snapshot `6034246` | `Assets/PolygonOffice` | content-only |
| Unity Localization | 1.5.12 | `Packages/com.unity.localization` | `Unity.Localization` |
| TextMeshPro | UGUI 2.5.0 | `Assets/TextMesh Pro`, package UGUI | `Unity.TextMeshPro` |

Версии подтверждены по установленным исходникам, а перечисленные ниже типы и методы — через reflection загруженных Unity-сборок.

`POLYGON Office` импортирован из `eyetengu/2024_March_Office` с сохранением
исходных `.meta`. В проект перенесён только vendor-каталог `Assets/PolygonOffice`;
сцены и настройки вне этого каталога, внешний demo-код и остальные наборы не
импортированы. Вложенные vendor sample-сцены сохранены как часть неизменённого
snapshot, но не являются зависимостями игры. Каталог считается read-only,
production-prefab'ы должны ссылаться на него из project-owned каталогов.

### POLYGON Office: структура и паттерн внедрения

- Unity 6000.5.6f1 распознаёт в каталоге 808 prefab'ов без ошибок импорта.
- Основные группы: `Buildings`, `Characters`, `Props`; для текущего эпизода нужны
  прежде всего модульная архитектура, Furniture, Desk Props, Misc, Roof Props и
  Wall Props. Персонажи из пака в офисный бой не добавляются.
- Существующие project-owned prefab'ы остаются владельцами gameplay-компонентов,
  Rigidbody, collider, trigger, pickup/break/reset-состояний и точек привязки.
- Synty-prefab подключается внутрь них как visual child. Прямая замена корневого
  prefab'а vendor-объектом запрещена: она сделает игровой контракт зависимым от
  структуры стороннего ассета.
- Для стен и пола сохраняются текущие непрерывные greybox-коллайдеры, а модульная
  геометрия Synty становится presentation-слоем. Это защищает ширину проходов,
  line of sight, броски и арену босса.
- Материалы vendor-каталога не перекрашиваются на месте. Если потребуется особая
  nightmare-палитра, создаются project-owned material variants или Material
  Property Block в `Assets/Game/Episodes/Office/**`.

### POLYGON Office: проверенный паттерн внедрения

Подтверждено на проверочном срезе art-pass (`polish/office-synty-art-pass`):

- Внедрением владеет editor-слой `OfficeArtPass`, который вызывается из
  `OfficeSceneBuilder`. Runtime-код эпизода о паке не знает.
- Vendor-prefab кладётся в контейнер `Synty Visual` внутри project-owned объекта, а
  его коллайдеры выключаются: `MeshCollider` есть почти у каждого prefab пака и без
  выключения он добавил бы вторую, чужую геометрию столкновений.
- Модели пака заметно мельче офисного greybox (стол `2.18 × 0.95` против
  `3.4 × 1.6`), поэтому каждая подгоняется под явно указанный greybox-объём. Иначе
  под коллайдером остаётся невидимый объём, а предметы на столах повисают в воздухе
  или тонут в столешнице.
- Материалы пака уже используют `Universal Render Pipeline/Lit` и в URP работают
  без правок, но их альбедо светлее офисной палитры. Приглушение делается
  project-owned material variants (`M_Synty_*.mat`), которые наследуют материал
  пака и переопределяют только `_BaseColor`.
- `MaterialPropertyBlock` для такой коррекции не подходит: он не сериализуется
  вместе со сценой.
- Pivot и ориентация у prefab пака не унифицированы: стеновые модули начинаются с
  края, а панель индикаторов серверного шкафа смотрит по локальному -X. Габариты и
  разворот проверяются по renderer bounds, а не предполагаются.

## Общие правила vendor-ассетов

- Vendor-каталоги считаются read-only. Проектный код интеграции размещается в `Assets/Game/Integrations/<Product>/`.
- Эпизод зависит от проектного интерфейса или адаптера, а не от конкретного vendor-prefab или компонента.
- Demo-сцены и demo-скрипты используются как справка, но не становятся зависимостями production-сцен.
- Обновление ассета выполняется в отдельной feature-ветке: резервная точка, импорт, компиляция, smoke test, затем осознанный merge.
- Общий graph, prefab, preset или каталог стилей одновременно имеет одного владельца. Бинарные и конфликтные Unity-ресурсы вручную не сливаются.

## NodeCanvas 3.42

### Разделение ролей

| Модуль | Использовать для | Не использовать для |
|---|---|---|
| FSM | фазы эпизода, режимы мини-игры, переходы между диалогом, исследованием и итогом | покадровая механика и сложные вычисления |
| Dialogue Tree | реплики, выборы, короткие cutscenes, ветвление и Sub Dialogue | авторитетное сохранение прогресса |
| Behaviour Tree | поведение NPC и противников: офисная техника, посетители бара | линейная режиссура сюжета |

Для фотолинии рекомендуемый верхний FSM: `Restore -> IntroDialogue -> Explore -> Camera -> Publish -> ReflectionDialogue -> Arrival -> Complete`. Названия — ориентир; checkpoint сохраняется на смысловой границе, а не на каждом внутреннем node.

### Graph ownership

- Asset Graph — стандарт для переиспользуемых и командных графов. Один `.asset` имеет одного владельца ветки.
- Bound Graph допустим для малого scene-local прототипа, которому нужны прямые ссылки на объекты сцены.
- Общая логика выносится в Sub Graph. Зависимости передаются через blackboard variables, а не через поиск объектов по имени.
- Стабильные имена переменных: `characterId`, `phase`, `checkpointId`, `choiceId`, `truth`, `reach`. Переименование считается изменением контракта.

### Blackboard и сохранения

- Blackboard хранит только временное runtime-состояние графа.
- Единственный владелец долговременного прогресса — существующий `GameSaveService`.
- При входе эпизод читает `TryGetCharacterCheckpoint`, преобразует payload в типизированный episode-state и заполняет blackboard.
- На безопасной сюжетной границе адаптер собирает только нужные поля и вызывает `SaveCharacterCheckpoint`.
- Не сохраняем произвольное положение выполнения node: после загрузки FSM входит в устойчивую фазу по `checkpointId`.
- Встроенные `Blackboard.Save/Load` не создают второй независимый слот сохранения.

### Связь графов с C#

- Custom `ActionTask`/`ConditionTask` — тонкий адаптер к проектному сервису. Правила урона, сохранения, экономики и разблокировки финала остаются в C#.
- Для переходов graph-to-code используются документированные Signal/Event с устойчивыми именами. Строковые имена не размножаются по сценам.
- Для core-контрактов предпочтительнее типизированная custom task, чем reflection-задача, хрупкая при переименовании API.
- Подтверждённые runtime API: `FSMOwner.TriggerState`, `DialogueTreeController.StartDialogue(...)`, `BehaviourTreeOwner.Tick`, методы `Blackboard.GetVariableValue/SetVariableValue`.
- Перед AOT-сборкой нужно создать AOT classes и `link.xml` через Preferred Types Editor NodeCanvas; для текущего Windows-прототипа это не блокирует разработку.

### Минимальный набор проектных адаптеров

Создавать только по мере появления реального сценария:

- `LoadEpisodeCheckpoint` / `SaveEpisodeCheckpoint`;
- `CompleteCharacterStoryLine`;
- `SetEpisodePhase` или типизированный переход FSM;
- `ShowGameFeedback` через проектный feedback-сервис;
- условия для проверок episode-state без копирования бизнес-логики в graph.

### Катсцены через NodeCanvas

- Project task `PlayCutsceneTask` принимает стабильный `cutsceneId` и ждёт
  результат общего `CutsceneDirector`.
- `characterId`, `startCheckpointId`, `completionCheckpointId` передаются как
  контекст и доступны обработчикам результата, но task не пишет save напрямую.
- Выходы `endReason` и `wasSkipped` позволяют графу выбрать reflection beat,
  применить state и вызвать отдельную task сохранения.
- Остановка graph отменяет только катсцену с тем же ID; завершённый callback не
  применяется повторно.

## Damage Numbers Pro 4.55

### Назначение и граница

Damage Numbers Pro — только presentation. Сначала C#-система изменяет авторитетное игровое состояние, затем публикует событие обратной связи. Графы NodeCanvas и игровые сущности обращаются к проектному `IGameFeedback`/`GameFeedbackService`, а не напрямую к DNP-prefab.

Для джема достаточно небольшого project-owned каталога семантических стилей:

- `DamageNormal`;
- `DamageCritical`;
- `HealOrSafety`;
- `ResourceLoss`;
- `NarrativeNotice`.

Цвет не должен быть единственным носителем смысла: используем знак, короткий текст или иконку. В сюжетных сценах избегаем спама эффектами.

### World и UI

- `DamageNumberMesh` и `Spawn(worldPosition, value/text)` — эффекты в мире.
- `DamageNumberGUI` и `SpawnGUI(rectParent, anchoredPosition, value/text)` — Canvas/HUD.
- Короткий эффект обычно остаётся в точке события; `SetFollowedTarget` применяется только когда эффект действительно должен следовать за объектом.

Проверенные на установленной версии вызовы:

```csharp
DamageNumber popup = worldPrefab.Spawn(position, value);
popup.SetFollowedTarget(target);

DamageNumber hudPopup = guiPrefab.SpawnGUI(parent, anchoredPosition, text);
```

### Pooling, комбинация и жизненный цикл

- На production-prefab включаются встроенный pooling и разумный `maxActiveInstances`.
- `Spawn` возвращает pooled instance. Любые изменения через `SetColor`, `SetScale`, `SetGradientColor` и подобные методы нужно полностью сбрасывать перед повторным использованием. Надёжнее отдельные семантические prefab/preset, чем множество мутаций после spawn.
- Если включена combination, задаётся стабильный spam group по цели и категории; для GUI объединяемые экземпляры должны иметь общего parent.
- События `OnSpawn`, `OnFadeOut`, `OnDespawn`, `OnAbsorb` относятся только к визуальному жизненному циклу и не запускают игровую логику.
- `disableOnSceneLoad` включён по умолчанию для проектных prefab, если переход между сценами не является явной частью эффекта.

### Применение в эпизодах

- Drive: изменение топлива, денег, здоровья/безопасности семьи и короткие предупреждения.
- Office: урон ожившей технике, критические попадания, перегрузка и цель взаимодействия.
- Photo: изменение `truth`/`reach`, лайки и последствия публикации — умеренно, чтобы интерфейс не превращался в аркадный шум.

## Совместное использование

Поток зависимости один:

`NodeCanvas task -> project service/interface -> gameplay state change -> feedback event -> Damage Numbers Pro adapter`.

Обратная зависимость запрещена: DNP не меняет state и не двигает NodeCanvas-граф. Сохранение вызывается отдельно на смысловом checkpoint после завершения операции.

Локализация подключается к NodeCanvas только через проектные tasks
`GetLocalizedStringTask` и `SetLocaleTask`. Graph хранит ключи, а не переводы.
TMP и Unity Localization не меняют gameplay-state и не владеют сохранением.

## Checklist интеграции

- [ ] Vendor-каталоги не изменены.
- [ ] У graph/prefab/preset назначен один владелец.
- [ ] Эпизод зависит от проектного адаптера.
- [ ] Blackboard не создаёт параллельный save-slot.
- [ ] Checkpoint восстанавливает устойчивую фазу эпизода.
- [ ] DNP-prefab использует pooling и лимит активных экземпляров.
- [ ] Pooled popup не наследует случайные настройки прошлого spawn.
- [ ] Unity Console не содержит новых ошибок и предупреждений.
- [ ] Основной сценарий проверен после остановки и продолжения игровой сессии.
