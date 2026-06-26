# Skill Rarity Drop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add skill rarity data, BattleMap-based S_Core skill drops, skill inventory equip/unequip, and RestRoom skill upgrade support.

**Architecture:** Keep master data in `GameData.xlsx` and runtime decisions in small C# services. Battle rewards add at most one skill per `BattleMapId`, skill inventory behaves like relic inventory, and upgrade eligibility is controlled by shared skill classification helpers.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing Excel-backed data loader, existing Battle reward and inventory UI patterns.

---

### Task 1: Classification And Drop Logic

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillMasterData.cs`
- Create: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRarityUtility.cs`
- Create: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRewardRoller.cs`
- Test: `Assets/Tests/EditMode/SkillRewardAndInventoryTests.cs`

- [ ] Add `SkillRarity` and `Rarity` to `SkillMasterData`.
- [ ] Add helper methods for upgrade eligibility, unequip eligibility, and core drop rarity checks.
- [ ] Add deterministic selection logic that rolls drop chance once, rolls rarity by configured weights, then picks one matching `S_Core_*` skill.
- [ ] Cover the helper and roller behavior with EditMode tests.

### Task 2: BattleMap Reward Integration

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Map/BattleMapData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/BattleMapDatabase.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardResolver.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardPanelUI.cs`

- [ ] Add skill drop chance and rarity weight fields to `BattleMapData`.
- [ ] Expose a first-row lookup per `BattleMapId` for map-level drop settings.
- [ ] Add `BattleRewardType.Skill`.
- [ ] Resolve one skill reward from the current map's `BattleMapId`.
- [ ] On claim, store the skill id in `BattleRuntimeData.SkillInventoryIds` and refresh skill inventory UI.

### Task 3: Skill Inventory And Equip

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryIconUI.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryPanelUI.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryEquipService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/EquippedSkillPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/EquippedSkillCharacterRowUI.cs`
- Modify: `Assets/Project/Scripts/UI/Battle/Canvas/EquippedSkillSlotUI.cs`

- [ ] Add skill inventory icon spawning under the assigned content transform.
- [ ] Select an inventory skill, then equip it to a free skill slot.
- [ ] Remove equipped inventory skills from inventory and return previous free-slot skills to inventory when allowed.
- [ ] Allow unequip for `Public` and `Core`, and block unequip for fixed/unique-style skills.
- [ ] Refresh all inventory/equipped panels after equip or unequip.

### Task 4: RestRoom Upgrade

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/SkillUpgradePanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/SkillUpgradeIconItem.cs`

- [ ] Include owned inventory skills as upgrade candidates.
- [ ] Restrict upgrades to `Ability`, `Public`, and `Core`.
- [ ] Replace inventory entries when upgrading inventory skills.
- [ ] Keep existing character slot upgrade behavior for eligible equipped skills.

### Task 5: Data Workbook

**Files:**
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameData.bytes`

- [ ] Add `Rarity` to `SkillMaster`.
- [ ] Add skill drop chance and rarity weights to `BattleMap`.
- [ ] Assign `Move`, `Passive`, `Unique`, `CharacterExclusive`, `Shared`, `CoreCommon`, `CoreRare`, and `CoreEpic` values.
- [ ] Copy the workbook to `GameData.bytes`.

### Task 6: Verification

**Files:**
- Use: `Assets/Tests/EditMode/SkillRewardAndInventoryTests.cs`

- [ ] Run the targeted EditMode tests if Unity batchmode is available.
- [ ] Run a compile-oriented check using the generated C# project if Unity tests cannot be executed.
- [ ] Inspect git diff to confirm no unrelated user edits were reverted.
