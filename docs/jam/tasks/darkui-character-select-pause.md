# DarkUI character select and pause menu

## Scope

- Apply DarkUI styling to `CharacterSelect` and the shared global pause menu.
- Add simultaneous 3D RenderTexture portraits to the three character choices.
- Reuse PolygonOffice character models without modifying vendor content.
- Do not change any Office episode HUD or gameplay UI.

## Acceptance

- [x] CharacterSelect shows three readable DarkUI choices with head portraits.
- [x] Each choice keeps its existing route, progress, and navigation behavior.
- [x] Global pause menu uses DarkUI button styling in non-menu gameplay scenes.
- [x] Office-owned UI files and `Prologue_Office` remain unchanged.
- [x] Unity compiles and affected scenes validate without errors.

## Handoff

- Added a project-owned shared DarkUI theme asset referencing vendor sprites.
- Added three PolygonOffice portrait rigs with isolated Cinemachine channels and runtime RenderTextures.
- Verified the character screen visually in Play Mode and used its Photo button to load `Prologue_Photo`.
- Verified the shared pause menu in `SampleScene`; its save availability contract remained active.
- No file under `Assets/Game/Episodes/Office/**` and no `Prologue_Office.unity` content was changed.
