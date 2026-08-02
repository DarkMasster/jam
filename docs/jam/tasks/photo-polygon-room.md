# Photo: PolygonOffice room, entrance and airport dioramas

- Owner: AI / Photo team
- Status: Done
- Priority: P1
- Branch: `feature/photo-polygon-room`
- Dependencies: Photo prologue vertical slice, PolygonOffice import
- Camera package: Cinemachine 3.1.7
- Timebox: 2-3 hours

## Goal

Replace the room, entrance and airport flat white-box stages with readable 3D dioramas while preserving the existing dialogue, choices, saves and NodeCanvas flow.

## Scope

- `Assets/Game/Episodes/Photo/**`
- `Assets/Game/Scenes/Prologue_Photo.unity`
- `docs/jam/STATE.md`, `docs/jam/BACKLOG.md`, this task file

`Assets/PolygonOffice/**` is read-only vendor content.

## Acceptance criteria

- [x] RoomSecret, RoomPhoto and MotherDialogue show three authored camera shots.
- [x] Room and portrait shots are selected through Cinemachine virtual cameras; Unity cameras only output to RenderTextures.
- [x] Existing TMP choices remain readable and functional.
- [x] No characters are present in the room diorama; a head-and-shoulders 3D portrait is rendered on a dark background at the left of the lower dialogue panel.
- [x] Leaving the room steps hides the room render.
- [x] Scene opens and scripts compile without new errors.
- [x] MailboxHunt, MailboxPublication and MailboxReaction use a dedicated entrance diorama and three Cinemachine shots.
- [x] The summons and butterfly are both readable in the mailbox composition.
- [x] AirportPhoto, BorderControl and Summary use a dedicated airport diorama and three Cinemachine shots.
- [x] The airport composition clearly reads as a terminal and passport-control booth without scene characters.

## Verification

Run `Jam/Photo/Create Polygon Room Diorama`, open `Prologue_Photo`, then play or continue the Photo line through the first three production steps.

## Handoff

- Done: runtime presenter, RenderTexture integration, deterministic room/entrance/airport builder, project-owned wrapper prefab and scene instance implemented; mailbox, marked summons, butterfly, terminal board, glass passport booth and stamped passport are visible in their compositions.
- Remaining: optional facial animation only if time remains after P0 work.
- Known issues: portraits use static humanoid bone poses rather than authored animation clips or lip-sync; airport glass uses an opaque stylized material for jam-build readability. The full manual run logs intermittent `referenced script (Unknown) ... missing` on scene transitions although a live scan of loaded scene GameObjects finds no null components. URP also reduces additional-light shadow resolution when the diorama and portrait lights are active.
- Verified: Unity 6000.5.3f1 compile, scene validation (0 issues), Play Mode room/entrance/airport screenshots, exact active camera pairs for all nine production steps, no Animator/character inside the airport diorama, and humanoid portrait poses. Manual UI run passed `Main -> New Game -> CharacterSelect -> Room -> Entrance -> Airport -> BorderControl -> Summary -> CharacterSelect`; pause-menu save at `photo.explore` and `Main -> Continue` restored `RoomPhoto` with the saved choice and correct camera.
- Last commit: not requested.
