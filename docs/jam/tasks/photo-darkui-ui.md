# Photo DarkUI UI

## Scope

- Адаптировать runtime-интерфейс истории третьего персонажа под общий DarkUI.
- Сохранить механику, локализацию, NodeCanvas Dialogue Trees и save-flow.
- Сохранить мятный/розовый как семантические цвета Честности/Признания.

## Acceptance

- [x] Story panel, stage и нижняя диалоговая панель используют DarkUI-спрайты.
- [x] Кнопки, варианты ответа и публикационные карточки используют DarkUI-рамку.
- [x] Портрет остаётся слева и рисуется поверх нижней панели.
- [x] Play Mode smoke-test входа в Photo не даёт новых ошибок.

## Handoff

- Изменения сосредоточены в runtime-конструкторе `PhotoWhiteboxController`.
- Сюжетные правила, checkpoints, Dialogue Trees, AudioCue и сцена не менялись.
- Unity перекомпилировал код без ошибок; вход в `Prologue_Photo` проверен в Play
  Mode, после проверки редактор возвращён на сцену `Main`.
