# Backlog

Приоритеты: `P0` — без этого игру нельзя отправить; `P1` — сильно улучшает игру;
`P2` — делается только при наличии запаса. Статусы: `Todo`, `Doing`, `Blocked`,
`Done`, `Cut`.

| Приоритет | Задача | Владелец | Ветка | Статус | Дедлайн |
|---|---|---|---|---|---|
| P0 | Создать единую спецификацию разработки | ИИ/продюсер | `feature/story-premortem` | Done | Готово |
| P0 | Назначить владельцев Core, Drive, Office, Photo и Narrative | Продюсер | `feature/team-ownership` | Todo | До кода |
| P0 | Ответить на сюжетные вопросы P0 | Продюсер | `feature/story-premortem` | Doing | До сценария |
| P0 | Утвердить тему `Reflection + Momentum` и структуру актов | Продюсер | `feature/story-premortem` | Done | Готово |
| P0 | Разрешить хронологию линии героини | Продюсер | `feature/story-premortem` | Done | Готово |
| P0 | Утвердить параметры трёх мини-игр Пролога | Продюсер | `feature/story-premortem` | Doing | До storyboard |
| P0 | Написать outline трёх линий Пролога | TBD | `feature/prologue-outline` | Blocked | Час 3 |
| P0 | Сделать общий диалоговый и state-каркас | TBD | `feature/narrative-core` | Todo | Час 6 |
| P0 | Сократить каждую мини-игру до одного повторяемого действия | TBD | `feature/minigame-scope` | Blocked | Час 2 |
| P0 | Написать по три сюжетных бита на героя | TBD | `feature/story-outline` | Blocked | Час 3 |
| P0 | Утвердить питч и core loop | Продюсер | `feature/game-concept` | Done | Готово |
| P0 | Выбрать движок, платформу и способ сборки | TBD | `feature/project-bootstrap` | Todo | Час 2 |
| P0 | Создать вертикальный прототип core loop | TBD | `feature/core-loop` | Todo | Час 12 |
| P0 | Главное меню, выбор героя и сохранение прогресса | Интегратор | `feature/main-menu` | Done | Готово |
| P0 | Актуализировать архитектуру визуальной новеллы с NodeCanvas и DNP | ИИ/интегратор | `feature/photo-novel-architecture` | Done | Готово |
| P0 | White-box запуск истории персонажа 3 из главного меню | ИИ/интегратор | `feature/photo-prologue-whitebox` | Done | Готово |
| P0 | Сохранение всей Photo-линии по актам, миграция v1 → v2 | ИИ/интегратор | `feature/photo-save-v2` | Done | Готово |
| P0 | Общий HUD, pause-меню и контракт сохранения игровых режимов | ИИ/интегратор | `feature/global-game-hud` | Done | Готово |
| P0 | Базовая гибридная система катсцен: NodeCanvas + Storyboard + Timeline | ИИ/интегратор | `feature/cutscene-foundation` | Done | Готово |
| P0 | Реализовать вертикальный срез фотопролога | TBD | `feature/photo-prologue` | Todo | Час 12 |
| P0 | Первый Unity-срез офисного кошмара | ИИ-агент | `feature/office-unity-scene` | Done | Час 12 |
| P0 | Создать и закрепить живой roadmap офисного эпизода | ИИ-агент | `feature/office-unity-scene` | Done | Готово |
| P0 | Офис `M1A`: предмет, бросок и простое разрушение | ИИ-агент | `feature/office-unity-scene` | Done | Готово |
| P0 | Офис `M1B`: противник, Momentum и быстрый restart | TBD | `feature/office-m1b-pressure` | Blocked | После P0 №7–8 |
| P0 | Добавить победу, поражение и рестарт | TBD | `feature/game-flow` | Todo | Час 24 |
| P0 | Получить проверенный финальный билд | TBD | `fix/final-build` | Todo | Час 42 |
| P1 | Добавить ключевую визуальную и звуковую обратную связь | TBD | `polish/game-feel` | Todo | Час 34 |
| P1 | Зафиксировать паттерны NodeCanvas и Damage Numbers Pro | ИИ/интегратор | `feature/integration-patterns` | Done | Готово |
| P1 | Провести три коротких build review | TBD | `feature/build-review` | Todo | 12/24/42 |
| P1 | Подготовить страницу, описание и скриншоты | TBD | `feature/submission` | Todo | Час 46 |
| P2 | Дополнительный контент | TBD | TBD | Todo | Только после P0 |

## WIP-лимит

- У одного участника одновременно не более одной задачи `Doing`.
- Новая задача не начинается, пока текущая не завершена, не передана или не
  переведена в `Blocked`.
- P2 не начинается, пока остаются незавершённые P0.
