# Паттерны сторонних интеграций

Последняя проверка: 2026-08-01
Среда проверки: Unity 6000.5.3f1, загруженные сборки Editor, Console без ошибок и предупреждений.

Этот документ — долговременная память о том, как команда использует купленные ассеты. Он фиксирует границы ответственности, чтобы эпизоды можно было делать параллельно и не связывать игровую логику с vendor-кодом.

## Установленные версии

| Инструмент | Версия | Vendor-каталог | Сборка |
|---|---:|---|---|
| NodeCanvas | 3.42 | `Assets/ParadoxNotion/NodeCanvas`, `Assets/ParadoxNotion/CanvasCore` | `NodeCanvas`, `ParadoxNotion` |
| Damage Numbers Pro | 4.55 | `Assets/DamageNumbersPro` | `DamageNumbersPro` |

Версии подтверждены по установленным исходникам, а перечисленные ниже типы и методы — через reflection загруженных Unity-сборок.

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
