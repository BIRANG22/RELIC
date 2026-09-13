# Battle UI Localization Repair

## Scope

Repair display-only localization boundaries in Battle UI. Combat commands, state, random, and networking are unchanged.

## Findings and direction

- Monster UI currently assigns `MonsterMasterData.SpecialAction*`, `MonsterSkillData.Name`, and `EffectDesc` directly to TMP fields. Route each through `GameDataLocalization` using stable MonsterId/SkillId keys.
- Reward equipment character labels assign `CharacterMasterData.Name` directly. Route through `GameDataLocalization.CharacterName`.
- Event titles and choices need a separate Event GameData key contract before mutation because the current Event sheet field names and repeated choice rows must be inspected first.
- Static prefab/scene labels require the Localization Manager after the YAML scanner fix; they must not be hard-wired to an arbitrary unrelated key.

## Verification

Compile the Editor and runtime assemblies. Execute EditMode tests in the open Unity Test Runner; batchmode is intentionally excluded.
