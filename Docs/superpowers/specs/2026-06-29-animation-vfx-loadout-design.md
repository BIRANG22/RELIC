# Animation/VFX and Loadout Cleanup Design

## Goal

Unify battle presentation and data ownership around the existing Excel-to-runtime pipeline.

Data should flow through:

`ExcelWorkbookReader -> ExcelSheetSelector -> CsvLoader -> DataBootstrap -> Database/DataManager -> Runtime`

The old loadout wrapper classes are removed because skill, rune, monster skill, master data, and runtime data already own the needed values.

## Scope

This work covers three connected changes:

- Remove `CharacterSkillLoadout`, `CharacterRuneLoadout`, and `MonsterSkillLoadoutData`.
- Load monster possible skill slots directly from the `Monster` sheet columns `PossSkillId01` through `PossSkillId10`.
- Select player and monster animation/VFX from stable presentation rules instead of ad hoc random or timeline-only rules.

## Data Model

`CharacterMasterData` keeps the existing Excel-backed fields:

- `PassiveSkill1`
- `UniqueSkill1`
- `CharacterSkill1`
- `CommonSkill1`
- `Rune1` through `Rune5`

`DefaultSkillLoadout` and `BuildSkillLoadout()` are removed. Character default skills and runes are read directly from the master fields or simple helper methods.

`CharacterEquipmentData` stores equipped data directly:

- `PassiveSkillId`
- `UniqueSkillId`
- `AbilitySkillId`
- `FreeSkillIds`
- `RuneIds`
- `FragmentIds`

`CharacterEquipmentManager` updates those direct fields and only ensures array lengths.

`MonsterMasterData` adds Excel-backed fields:

- `PossSkillId01`
- `PossSkillId02`
- `PossSkillId03`
- `PossSkillId04`
- `PossSkillId05`
- `PossSkillId06`
- `PossSkillId07`
- `PossSkillId08`
- `PossSkillId09`
- `PossSkillId10`

Values of `0`, empty, or whitespace mean no skill. A helper returns the normalized possible skill IDs while preserving their 1-based action slot positions.

`MonsterRuntimeData.PossSkillIds` is populated from `MonsterMasterData` when a monster runtime is created.

## Player Presentation

Player skills choose animation/VFX from `SkillMasterData.SkillType`:

- `SkillType.Power` uses one Power animation/VFX slot.
- `SkillType.Attack` uses Attack animation/VFX slots 1 through 3.
- `SkillType.Skill` uses one Skill animation/VFX slot.

Move, guard, hit, and dead presentation remains separate.

## Monster Presentation

`MonsterReservedCommand` stores `ActionIndex` from 1 through 10.

The action index is resolved by matching `command.SkillId` against the monster master data columns `PossSkillId01` through `PossSkillId10`.

`BattleUnitAnimator` gets monster action slots 1 through 10. Each slot has ready state, action state, and VFX entry. Monster execution calls the animator with the reserved command so it can use `ActionIndex`.

If an action index is missing or invalid, the animator falls back to action 1 so the battle can continue.

## Buff/Debuff VFX

Buff/debuff VFX should play when the status is actually applied to the target, not when the caster uses a skill.

The trigger point is:

- `BattleEffectUtility.AddStatusToPlayer`
- `BattleEffectUtility.AddStatusToMonster`

The target unit's `BattleUnitAnimator` plays buff VFX for buff effects and debuff VFX for debuff/abnormal effects.

Caster skill presentation stays separate from target status presentation.

## Implementation Notes

The change should stay compatible with existing Excel loaders:

- Use `MonsterCsvLoader` aliases only if needed for `PossSkillId01` naming variants.
- Keep `DataBootstrap` as the only data loading coordinator.
- Keep `DataManager` as the access point for databases and runtime stores.

The loadout files should be deleted only after all compile references are removed.

## Tests

Add or update EditMode tests for:

- `MonsterCsvLoader` maps `PossSkillId01` through `PossSkillId10`, preserving `0` as empty.
- `MonsterMasterData` normalizes possible skill IDs and preserves action slot positions.
- `MonsterReservedCommand` resolves `ActionIndex` from the monster possible skill slots.
- `BattleUnitAnimator` exposes methods that can play player Power/Attack/Skill presentation paths.
- Status application calls target-side buff/debuff presentation rather than caster-side presentation.

MSBuild should pass after each major slice. Unity EditMode tests should be run when the project is not already open in another Unity instance.
