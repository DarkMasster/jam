# English default locale

## Scope

- Сделать английский стартовым языком для нового пользователя.
- Не перезаписывать уже сохранённый выбор языка.

## Acceptance

- [x] Все runtime fallback-значения locale используют `en`.
- [x] `jam.settings.locale` по-прежнему имеет приоритет.
- [x] Unity компилируется без ошибок.

## Handoff

- Изменены согласованные defaults в `UnityLocalizationService`,
  `LocalizationBootstrap` и `Loc`.
- Существующие сохранения с `ru` останутся на русском до ручного переключения.
