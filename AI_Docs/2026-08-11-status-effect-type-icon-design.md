# Status Effect Type Icon Design

## Context

`GameData.xlsx` and `Assets/Resources/Data/GameDataRuntime.csv` now include an `Effect` sheet column named `EffectType`.
Runtime loading already selects the `Effect` sheet through `EffectCsvLoader`, but `EffectMasterData` does not expose the field, so the value is ignored.

`StatusEffectIcon` currently asks `StatusEffectIconDatabase` for a sprite by `EffectId`. The prefab has separate child images: `IconImage` for the existing status icon, and `Image` for the type badge/background image requested here.

## Design

- Add an `EffectType` enum with `Neutral`, `Beneficial`, and `Harmful`.
- Add `EffectMasterData.EffectType` so `DataRowMapper` reads the new column automatically.
- Keep `IconImage` responsible for the existing per-effect status icon.
- Add a separate type icon reference that points to the child named `Image`.
- Move type-based icon selection into `StatusEffectIconDatabase`.
- Connect `icon_buff.png` and `icon_debuff.png` to `StatusEffectIconDatabase.asset`.

## Behavior

- `Beneficial` effects use `icon_buff`.
- `Harmful` effects use `icon_debuff`.
- `Neutral` effects return no type sprite, keeping the child `Image` disabled.
- `IconImage` continues to use the existing manual per-effect database mapping.
- The type `Image` uses only `EffectType`; if the effect id is not found in `EffectDatabase`, the type image stays empty.
