# Testing Log

Manual, because there is nothing here a console runner could exercise — see
[RELEASING.md](RELEASING.md).

## Confirmed working

| Behaviour | Notes |
|---|---|
| World hover finds plants | Raycast, with the game's interaction target preferred |
| Crop naming | *Grapes* rather than *Grape Seeds*, via the produce item |
| Watered state | Reads the tile's waterable persistence for the crop's required water type |
| Hidden until expanded | Nothing drawn at rest; Left Alt reveals |
| Game nameplate banner | Plum `#4A2E8F`, matching Chest Labels |

## Found by testing, then fixed

| Symptom | Cause |
|---|---|
| Extra detail never appeared | `KeyboardShortcut.IsPressed()` is false while any other key is held — so it never fired while walking |
| A changed default did nothing | BepInEx keeps existing values; a new default only reaches a fresh config file. Hit twice: `Detail` and `NameplateTint` |
| Panel looked nothing like Chest Labels | Was drawing a self-made plate; Chest Labels uses the game's nameplate banner |
| Banner was orange | `NameplateTint` left blank, and the game's own colour there is the NPC-name orange |
| Chopped trees read "fully grown" | A stump stage has no exits, and "fully grown" meant "no paths out" |
| Mature trees read "stage 5 of 6" | The 6th stage is the stump. `CachedGrowStages` is not a growth chain |
| Picked vines read "fully grown" | A regrowing crop keeps its final stage; needed `HarvestStatusPersistence.DayLastHarvested` |
| Weeds read "stage 1 of 1" | Single-stage growables were reporting a fact about the data model |
| "needs room to grow" on healthy trees | Satisfied requirements were listed under "needs", and a *spread* path's blocked footprint was being read as a growth blocker |
| Mod Menu descriptions overflowed | Config descriptions were paragraphs; they are now one line each |

## Still to verify

- [ ] **Fresh install** — delete the config, launch, check defaults and Mod Menu rendering
- [ ] **`Font:` log line** — confirms Gelica resolves; matters for the fallback plate
- [ ] **Stage costs** — with `VerboseLogging`, hover one of each crop. The dump prints per-stage
      day costs, which is the last unverified assumption: that they are in growth order
- [ ] **Save untouched** — back up, play a day hovering plants, diff
- [ ] **Herb gardens** — untested; they use `HerbGardenPersistence.HerbFarmTile` rather than a
      plain waterable, so the watered line may be absent there
- [ ] **Magic-water crops** — the required water type is read, but no magic-water crop has been
      hovered yet
