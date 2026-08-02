# DarkUI main menu

## Scope

- Restyle the existing runtime-built main menu with imported DarkUI sprites.
- Preserve new game, continue, language switching, quit, localization, and save behavior.
- Keep third-party files under `Assets/Dark UI/` unchanged.

## Acceptance

- [x] Main scene opens without compile or console errors.
- [x] Main menu uses DarkUI button and icon assets.
- [x] New Game opens character selection.
- [x] Continue remains enabled only for a valid save.
- [x] Language and Quit controls retain their behavior.
- [x] Layout is readable at the 1920x1080 reference resolution.

## Handoff

- Added a two-column DarkUI presentation to the runtime-built `Main` menu.
- Assigned vendor sprites through a project-owned editor setup command; `Assets/Dark UI/**` remains unchanged.
- Unity 6000.5.3f1 compiled with zero console errors.
- Play Mode screenshot was reviewed at 1280x720 (scaled from the 1920x1080 reference layout).
- Invoking `NewGameButton` loaded `Assets/Game/Scenes/CharacterSelect.unity`.
- Continue state still comes from `GameSaveService`; language and quit callbacks were not changed.
