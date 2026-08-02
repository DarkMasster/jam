# Office DarkUI HUD

## Scope

- Apply the shared main-menu DarkUI visual language to the Office gameplay HUD.
- Preserve all gameplay bindings, text, localization, momentum values and failure behavior.
- Make the change in `OfficeSceneBuilder` so future rebuilds retain it.

## Acceptance

- [x] Objective, coach, momentum and status panels use DarkUI sprites and palette.
- [x] Momentum and integrity remain readable during gameplay.
- [x] Failure overlay uses a DarkUI center frame.
- [x] Office gameplay contracts and serialized bindings remain unchanged.
- [x] Scene validates and Play Mode has no new errors.

## Handoff

- Updated the canonical `OfficeSceneBuilder`, then rebuilt `OfficeHud.prefab` and `Prologue_Office`.
- Reapplied the existing 3D storyboard presentation after the canonical Office rebuild.
- Visually checked the regular HUD and forced failure overlay at 1280x720/960x540.
- No gameplay, save, input, camera-follow or collision contract was changed.
- `RenderedPortrait` is moved to the last sibling after storyboard UI construction,
  so the 3D portrait renders above the dialogue panel instead of being clipped by it.
