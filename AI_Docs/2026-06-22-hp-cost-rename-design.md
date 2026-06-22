# HP/Cost Rename and Monster Skill Tooltip Design

## Goal
Unify gameplay data and code naming around `HP` and `Cost`, while keeping import compatibility for old `Health` and `Stamina` column names. Update monster timeline previews, random monster attack damage, and player skill tooltip calculation display.

## Decisions
- Rename public runtime/master data fields from `Health`/`Stamina` naming to `HP`/`Cost` naming in source code and Excel headers.
- Keep data-loader aliases so old `Health`, `MaxHealth`, `Stamina`, and `MaxStamina` columns still map into the new fields.
- Use `MonsterSkill.ValueRandomRange` from the existing Korean `수치값변수` column to calculate monster attack damage as `ValueRate - range` through `ValueRate + range`.
- Show monster timeline hover text from `MonsterSkill.EffectDesc`, with `"수치"` replaced by the computed random damage range.
- Use `SkillRangeIconDatabase` for monster action icons by `RangeId`, falling back to timeline action icons if the range icon is missing.
- Add a focused skill tooltip formatter so formulas such as `{(3+집중)x소모량}` display the calculated value for the current reservation cost and current Focus/Power stacks.

## Main Files
- `Assets/ExcelSource/GameData.xlsx`: rename visible English headers and text values to HP/Cost terms.
- `Assets/Resources/Data/GameData.bytes`: keep in sync with the workbook.
- `Assets/Project/Scripts/Gameplay/Data/Runtime/CharacterRuntimeData.cs`: rename runtime cost and HP fields/properties.
- `Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs`: rename max stat fields.
- `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterMasterData.cs`: rename monster master HP field.
- `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillData.cs`: add `ValueRandomRange`.
- `Assets/Project/Scripts/Gameplay/Data/Loaders/*`: add compatibility aliases before mapping.
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`: range icon lookup and monster desc formatting.
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleDamageService.cs`: random monster damage helper.
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`: use per-hit random monster damage for damage effects.
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Skill/SkillListSlotUI.cs`: use calculated tooltip text.
- `Assets/Tests/EditMode/BattleActionRegressionTests.cs`: add regression tests for aliases, tooltip formatting, timeline icons/descs, and random damage range.

## Verification
- Run targeted EditMode tests for the new behavior.
- Run a compile/build check if Unity test runner is available from the command line.
- Inspect changed workbook headers and a few key data rows after editing.
