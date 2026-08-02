# Episode storyboard dioramas

## Scope

- Convert all existing Office and Photo storyboard cutscenes to authored 3D stage shots.
- Keep cutscene IDs, story logic, skip behavior and save boundaries unchanged.
- Show speakers as separate dark-background 3D RenderTexture portraits.

## Acceptance

- [x] `office.prologue.setup` uses a 3D stage and portrait.
- [x] `office.prologue.awakening` uses a 3D stage and portrait.
- [x] `photo.prologue.intro` uses the existing Photo diorama and portrait rig.
- [x] `photo.prologue.to_be_continued` uses airport shots and portrait rig.
- [x] Existing sprite storyboard assets remain compatible.
- [x] Office and Photo scenes validate without missing scripts or broken prefabs.

## Handoff

- Added optional scene presenters to all four existing Office/Photo storyboard presentations.
- Office uses isolated exposition/awakening dioramas and Cinemachine channels without changing gameplay cameras or HUD.
- Photo reuses the production room/airport diorama and existing heroine portrait rig.
- Controlled Play Mode visually confirmed active Office Setup and Photo Intro; Photo Outro was started successfully by its stable ID.
