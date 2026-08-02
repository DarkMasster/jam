# Drive exposition scene

## Scope

- Create `Prologue_Drive` as a production-bound exposition scene.
- Use PolygonOffice environment props, Cinemachine shots and a RenderTexture portrait.
- Keep characters out of the stage diorama; show the protagonist only in the lower dialogue panel.
- Return to CharacterSelect after the current exposition-only slice.

## Acceptance

- [x] CharacterSelect loads `Prologue_Drive` without fallback.
- [x] Four exposition frames use authored 3D shots and a dark-background portrait.
- [x] Cutscene supports advance and skip.
- [x] Completion writes `drive.departure` and returns to CharacterSelect.
- [x] Unity compiles and the scene validates without errors.

## Handoff

- Added `Prologue_Drive` to Build Settings and kept `CharacterSelect` routing unchanged.
- Added a four-frame Moscow exposition with PolygonOffice props and isolated Cinemachine stage/portrait channels.
- Extended the shared storyboard presentation with an optional `IStoryboardScenePresenter`; existing sprite storyboards remain compatible.
- Play Mode test advanced all four frames, wrote `drive.departure`, and returned to `CharacterSelect`.
- No Drive gameplay beyond the exposition boundary is included in this slice.
