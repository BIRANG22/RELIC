# HP/Cost Rename Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename gameplay stat naming to HP/Cost, preserve old data import aliases, and update monster timeline/damage/tooltips.

**Architecture:** Keep runtime behavior in existing Unity services. Add small helper methods for aliasing, monster random damage range, and tooltip text formatting so high-risk UI and damage code remain testable.

**Tech Stack:** Unity C#, NUnit EditMode tests, OpenXML workbook edits via bundled spreadsheet runtime.

---

### Task 1: Add Failing Regression Tests

**Files:**
- Modify: `Assets/Tests/EditMode/BattleActionRegressionTests.cs`

- [ ] Add tests proving old `Health/Stamina` headers map to new HP/Cost fields through `DataRowMapper`.
- [ ] Add tests proving `MonsterSkillData.ValueRandomRange` maps from `수치값변수` and builds a `ValueRate +/- range` description.
- [ ] Add tests proving monster damage rolls stay within the configured range.
- [ ] Add tests proving skill tooltip formulas display calculated values for cost, focus, and power.
- [ ] Run the targeted EditMode tests and confirm the new tests fail before implementation.

### Task 2: Rename Data Fields With Import Aliases

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/CharacterRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Loaders/CharacterCsvLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Loaders/MonsterCsvLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Loaders/MonsterSkillCsvLoader.cs`

- [ ] Rename master/runtime fields to `MaxHP`, `MaxCost`, `CostRecovery`, `CurrentHP`, `CurrentCost`, reserved HP/Cost fields, and monster `HP`.
- [ ] Keep obsolete compatibility properties only where Unity/C# references would otherwise break during migration, and route them to the new fields.
- [ ] Add loader aliases from `MaxHealth -> MaxHP`, `Health -> HP`, `MaxStamina -> MaxCost`, `StaminaRecovery -> CostRecovery`, and `수치값변수 -> ValueRandomRange`.
- [ ] Run the targeted tests and confirm alias tests pass.

### Task 3: Update Source References

**Files:**
- Modify: all `Assets/Project/Scripts/**/*.cs` references found by `rg "Health|Stamina|CurrentHealth|CurrentStamina|MaxHealth|MaxStamina"`

- [ ] Replace internal source references with the new HP/Cost names.
- [ ] Update user-facing strings from `Stamina` or `스태미나` to `Cost` where they refer to the spendable resource.
- [ ] Keep effect ids such as `E_Max_Hp` only if changing ids would break existing content references.
- [ ] Run targeted tests and fix compile errors from stale names.

### Task 4: Monster Timeline Icon and Description

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`

- [ ] Change monster `SkillIcon` to use `SkillRangeIconDatabase.TryGetIcon(MonsterSkillData.RangeId)` first.
- [ ] Fall back to `ActionTypeIconDatabase` when no range icon exists.
- [ ] Format `MonsterSkillData.EffectDesc` by replacing `"수치"` with the random damage range for attack skills.
- [ ] Run tests for icon fallback and description formatting.

### Task 5: Monster Random Damage

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleDamageService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`

- [ ] Add helpers that calculate monster damage min/max and roll inclusive random damage from `ValueRate +/- ValueRandomRange`.
- [ ] Use the rolled value for monster `E_Strike` and `E_Pierce` damage contexts.
- [ ] Use the same roll helper for the monster dash attack path.
- [ ] Run random damage range tests repeatedly enough to cover min/max deterministically where possible.

### Task 6: Player Skill Tooltip Calculation

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillTooltipFormatter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Skill/SkillListSlotUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`

- [ ] Implement a formatter that replaces formula braces with calculated values using skill effect entries, reservation pay amount, Focus stack, and Power stack.
- [ ] Use the formatter in skill list hover detail text and timeline player skill description.
- [ ] Preserve original text when no runtime/command context exists.
- [ ] Run tooltip tests.

### Task 7: Update Workbook and Bytes

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameData.bytes`

- [ ] Rename relevant English headers and values to HP/Cost names.
- [ ] Rename `MonsterSkill` column M English header to `ValueRandomRange`.
- [ ] Update visible Korean/English resource wording in relevant descriptions.
- [ ] Save the workbook and copy it to `GameData.bytes`.
- [ ] Inspect workbook sheets to verify headers and sample rows.

### Task 8: Final Verification

**Files:**
- Check: `Assets/Tests/EditMode/BattleActionRegressionTests.cs`
- Check: changed C# files
- Check: `Assets/ExcelSource/GameData.xlsx`

- [ ] Run targeted EditMode tests.
- [ ] Run a broader compile/test command if available.
- [ ] Scan for stale user-facing `Stamina`/`Health` names in project scripts and workbook.
- [ ] Report exact verification commands and any remaining limitations.

## Self-Review
- Coverage: all requested rename, timeline hover/icon, monster random damage, and tooltip calculation requirements have a task.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: the plan consistently uses `HP`, `Cost`, and `ValueRandomRange` as the new source names.
