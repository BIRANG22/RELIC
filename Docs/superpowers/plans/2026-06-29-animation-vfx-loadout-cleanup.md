# Animation/VFX Loadout Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove redundant loadout wrapper models and route player/monster/status animation VFX through explicit master/runtime data.

**Architecture:** Excel fields remain the source of master data, loaders populate master objects, databases expose master objects through `DataManager`, and runtime objects copy only the state needed during battle. `BattleUnitAnimator` becomes the presentation selector: player commands select by `SkillType`, monster commands select by `ActionIndex`, and status VFX plays on the target when a status is applied.

**Tech Stack:** Unity 6000.0.68f1, C#, NUnit EditMode tests, MSBuild.

---

## File Structure

- Modify `Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs`: remove `DefaultSkillLoadout`, remove `BuildSkillLoadout()`, keep direct Excel-backed fields and rune helper.
- Modify `Assets/Project/Scripts/Gameplay/Data/Character/CharacterEquipmentData.cs`: replace loadout wrapper fields with direct skill/rune arrays.
- Modify `Assets/Project/Scripts/Gameplay/Data/Managers/CharacterEquipmentManager.cs`: write direct fields and ensure arrays.
- Modify `Assets/Project/Scripts/Gameplay/Data/Database/CharacterDatabase.cs`: stop calling `BuildSkillLoadout()`.
- Delete `Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs` and `.meta`.
- Delete `Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs` and `.meta`.
- Delete `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs` and `.meta`.
- Modify `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterMasterData.cs`: add `PossSkillId01` through `PossSkillId10` and helper methods.
- Modify `Assets/Project/Scripts/Gameplay/Data/Runtime/MonsterRuntimeData.cs`: copy normalized skill list and fixed 10-slot skill array from master data.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/MonsterReservedCommand.cs`: add `ActionIndex`.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleMonsterTurnPlanner.cs`: rely on command action index and keep range planning unchanged.
- Create `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitActionPresentation.cs`: serializable ready/action/vfx slot data.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`: add player Power/Skill slots, monster action 1-10 slots, target status VFX methods.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`: pass `MonsterReservedCommand` to monster animation calls.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectUtility.cs`: trigger buff/debuff VFX on actual status application.
- Create `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`: focused regression tests for this plan.

---

### Task 1: Remove Loadout Wrapper Models

**Files:**
- Create: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Character/CharacterEquipmentData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Managers/CharacterEquipmentManager.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/CharacterDatabase.cs`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs.meta`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs.meta`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs`
- Delete: `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs.meta`
- Test: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`

- [ ] **Step 1: Write failing loadout cleanup tests**

Add this file:

```csharp
using System;
using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class AnimationVfxLoadoutCleanupTests
{
    [Test]
    public void LoadoutWrapperTypes_AreRemoved()
    {
        Assert.That(FindType("Relic.Gameplay.Data.CharacterSkillLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.CharacterRuneLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.MonsterSkillLoadoutData"), Is.Null);
    }

    [Test]
    public void CharacterEquipmentManager_WritesDirectEquipmentFields()
    {
        CharacterEquipmentManager manager = new();

        manager.EquipPassive("C_Test", "Passive_01");
        manager.EquipUnique("C_Test", "Unique_01");
        manager.EquipAbility("C_Test", "Ability_01");
        manager.EquipFreeSkill("C_Test", 1, "Free_02");
        manager.EquipRune("C_Test", 4, "Rune_05");
        manager.EquipFragment("C_Test", 3, "Fragment_04");

        CharacterEquipmentData equipment = manager.GetOrCreate("C_Test");

        Assert.That(equipment.PassiveSkillId, Is.EqualTo("Passive_01"));
        Assert.That(equipment.UniqueSkillId, Is.EqualTo("Unique_01"));
        Assert.That(equipment.AbilitySkillId, Is.EqualTo("Ability_01"));
        Assert.That(equipment.FreeSkillIds, Has.Length.EqualTo(2));
        Assert.That(equipment.FreeSkillIds[1], Is.EqualTo("Free_02"));
        Assert.That(equipment.RuneIds, Has.Length.EqualTo(5));
        Assert.That(equipment.RuneIds[4], Is.EqualTo("Rune_05"));
        Assert.That(equipment.FragmentIds, Has.Length.EqualTo(4));
        Assert.That(equipment.FragmentIds[3], Is.EqualTo("Fragment_04"));
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL because `CharacterEquipmentData.PassiveSkillId`, `UniqueSkillId`, `AbilitySkillId`, `FreeSkillIds`, and `RuneIds` do not exist yet, or because loadout wrapper types still exist.

- [ ] **Step 3: Flatten character equipment data**

Replace `CharacterEquipmentData` fields with direct fields:

```csharp
using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterEquipmentData
    {
        public string CharacterId;

        public string PassiveSkillId;
        public string UniqueSkillId;
        public string AbilitySkillId;
        public string[] FreeSkillIds = new string[2];
        public string[] RuneIds = new string[5];
        public string[] FragmentIds = new string[4];
    }
}
```

- [ ] **Step 4: Update character equipment manager**

Replace the manager with direct field access:

```csharp
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class CharacterEquipmentManager
    {
        private readonly Dictionary<string, CharacterEquipmentData> equipmentMap = new();

        public CharacterEquipmentData GetOrCreate(string characterId)
        {
            if (!equipmentMap.TryGetValue(characterId, out CharacterEquipmentData equipment))
            {
                equipment = new CharacterEquipmentData { CharacterId = characterId };
                EnsureArrays(equipment);
                equipmentMap[characterId] = equipment;
            }

            EnsureArrays(equipment);
            return equipment;
        }

        public void EquipPassive(string characterId, string passiveId)
        {
            GetOrCreate(characterId).PassiveSkillId = passiveId;
        }

        public void EquipUnique(string characterId, string uniqueId)
        {
            GetOrCreate(characterId).UniqueSkillId = uniqueId;
        }

        public void EquipAbility(string characterId, string abilityId)
        {
            GetOrCreate(characterId).AbilitySkillId = abilityId;
        }

        public void EquipFreeSkill(string characterId, int slotIndex, string skillId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.FreeSkillIds.Length)
                return;

            equipment.FreeSkillIds[slotIndex] = skillId;
        }

        public void EquipRune(string characterId, int slotIndex, string runeId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.RuneIds.Length)
                return;

            equipment.RuneIds[slotIndex] = runeId;
        }

        public void EquipFragment(string characterId, int slotIndex, string fragmentId)
        {
            CharacterEquipmentData equipment = GetOrCreate(characterId);

            if (slotIndex < 0 || slotIndex >= equipment.FragmentIds.Length)
                return;

            equipment.FragmentIds[slotIndex] = fragmentId;
        }

        private void EnsureArrays(CharacterEquipmentData equipment)
        {
            if (equipment == null)
                return;

            if (equipment.FreeSkillIds == null || equipment.FreeSkillIds.Length != 2)
                equipment.FreeSkillIds = new string[2];

            if (equipment.RuneIds == null || equipment.RuneIds.Length != 5)
                equipment.RuneIds = new string[5];

            if (equipment.FragmentIds == null || equipment.FragmentIds.Length != 4)
                equipment.FragmentIds = new string[4];
        }
    }
}
```

- [ ] **Step 5: Remove character default loadout wrapper from master/database**

In `CharacterMasterData`, delete `DefaultSkillLoadout` and `BuildSkillLoadout()`. Keep direct Excel fields and this rune helper:

```csharp
public string[] GetRuneIds()
{
    return new string[]
    {
        Rune1,
        Rune2,
        Rune3,
        Rune4,
        Rune5
    };
}
```

In `CharacterDatabase.Initialize`, replace the method body with:

```csharp
public void Initialize(IEnumerable<CharacterMasterData> list)
{
    db.Initialize(list, x => x.CharacterId);
}
```

- [ ] **Step 6: Delete loadout wrapper files**

Delete these files:

```text
Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs
Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs.meta
Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs
Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs.meta
Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs
Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs.meta
```

- [ ] **Step 7: Run test to verify it passes**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: PASS with existing warnings only (`System.Net.Http` conflict and `Slash_Manager.usingSlashCircle` if still present).

- [ ] **Step 8: Commit**

Run if `git` is available:

```bash
git add Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs Assets/Project/Scripts/Gameplay/Data/Character/CharacterEquipmentData.cs Assets/Project/Scripts/Gameplay/Data/Managers/CharacterEquipmentManager.cs Assets/Project/Scripts/Gameplay/Data/Character/CharacterMasterData.cs Assets/Project/Scripts/Gameplay/Data/Database/CharacterDatabase.cs Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs Assets/Project/Scripts/Gameplay/Data/Common/CharacterSkillLoadout.cs.meta Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs Assets/Project/Scripts/Gameplay/Data/Common/CharacterRuneLoadout.cs.meta Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs Assets/Project/Scripts/Gameplay/Data/Monster/MonsterSkillLoadoutData.cs.meta
git commit -m "refactor: remove loadout wrapper data models"
```

Expected: commit succeeds. If `git` is unavailable in the worker environment, record that and continue.

---

### Task 2: Add Monster Possible Skill Slots and ActionIndex

**Files:**
- Modify: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Monster/MonsterMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/MonsterRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/MonsterReservedCommand.cs`
- Test: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`

- [ ] **Step 1: Add failing monster skill slot tests**

Append these tests to `AnimationVfxLoadoutCleanupTests`:

```csharp
[Test]
public void MonsterMasterData_NormalizesPossibleSkillSlotsAndPreservesActionIndex()
{
    MonsterMasterData master = new()
    {
        MonsterId = "M_Slots",
        HP = 10,
        PossSkillId01 = "S_Monster_A",
        PossSkillId02 = "0",
        PossSkillId03 = "",
        PossSkillId10 = "S_Monster_J"
    };

    Assert.That(master.GetPossibleSkillIds(), Is.EqualTo(new[] { "S_Monster_A", "S_Monster_J" }));
    Assert.That(master.GetPossibleSkillIdAtActionIndex(1), Is.EqualTo("S_Monster_A"));
    Assert.That(master.GetPossibleSkillIdAtActionIndex(10), Is.EqualTo("S_Monster_J"));
    Assert.That(master.GetActionIndexForSkill("S_Monster_J"), Is.EqualTo(10));
    Assert.That(master.GetActionIndexForSkill("Missing"), Is.EqualTo(0));
}

[Test]
public void MonsterRuntimeData_CopiesPossibleSkillSlotsFromMaster()
{
    MonsterMasterData master = new()
    {
        MonsterId = "M_Runtime_Slots",
        Name = "Runtime Slots",
        HP = 10,
        PossSkillId01 = "S_Monster_A",
        PossSkillId05 = "S_Monster_E",
        PossSkillId10 = "0"
    };

    MonsterRuntimeData runtime = new("Runtime_01", master);

    Assert.That(runtime.PossSkillIds, Is.EqualTo(new[] { "S_Monster_A", "S_Monster_E" }));
    Assert.That(runtime.GetActionIndexForSkill("S_Monster_E"), Is.EqualTo(5));
}

[Test]
public void MonsterReservedCommand_ResolvesActionIndexFromRuntimeSkillSlots()
{
    MonsterMasterData master = new()
    {
        MonsterId = "M_Command_Slots",
        Name = "Command Slots",
        HP = 10,
        PossSkillId04 = "S_Monster_Action04"
    };
    MonsterRuntimeData runtime = new("Runtime_Command", master);
    MonsterSkillData skill = new() { SkillId = "S_Monster_Action04" };

    MonsterReservedCommand command = new(runtime, skill);

    Assert.That(command.ActionIndex, Is.EqualTo(4));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL because `PossSkillId01` through `PossSkillId10`, monster helper methods, runtime `GetActionIndexForSkill`, and command `ActionIndex` are missing.

- [ ] **Step 3: Add monster master skill slot fields and helpers**

Add these members inside `MonsterMasterData`:

```csharp
public string PossSkillId01;
public string PossSkillId02;
public string PossSkillId03;
public string PossSkillId04;
public string PossSkillId05;
public string PossSkillId06;
public string PossSkillId07;
public string PossSkillId08;
public string PossSkillId09;
public string PossSkillId10;

public const int PossibleSkillSlotCount = 10;

public string[] GetPossibleSkillIdSlots()
{
    return new[]
    {
        NormalizePossibleSkillId(PossSkillId01),
        NormalizePossibleSkillId(PossSkillId02),
        NormalizePossibleSkillId(PossSkillId03),
        NormalizePossibleSkillId(PossSkillId04),
        NormalizePossibleSkillId(PossSkillId05),
        NormalizePossibleSkillId(PossSkillId06),
        NormalizePossibleSkillId(PossSkillId07),
        NormalizePossibleSkillId(PossSkillId08),
        NormalizePossibleSkillId(PossSkillId09),
        NormalizePossibleSkillId(PossSkillId10)
    };
}

public string[] GetPossibleSkillIds()
{
    string[] slots = GetPossibleSkillIdSlots();
    List<string> result = new();

    for (int i = 0; i < slots.Length; i++)
    {
        if (!string.IsNullOrWhiteSpace(slots[i]))
            result.Add(slots[i]);
    }

    return result.ToArray();
}

public string GetPossibleSkillIdAtActionIndex(int actionIndex)
{
    string[] slots = GetPossibleSkillIdSlots();
    int index = actionIndex - 1;

    if (index < 0 || index >= slots.Length)
        return "";

    return slots[index];
}

public int GetActionIndexForSkill(string skillId)
{
    if (string.IsNullOrWhiteSpace(skillId))
        return 0;

    string[] slots = GetPossibleSkillIdSlots();

    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i] == skillId)
            return i + 1;
    }

    return 0;
}

private static string NormalizePossibleSkillId(string skillId)
{
    if (string.IsNullOrWhiteSpace(skillId))
        return "";

    string trimmed = skillId.Trim();
    return trimmed == "0" ? "" : trimmed;
}
```

Also add `using System.Collections.Generic;` to the file.

- [ ] **Step 4: Copy master skill slots into monster runtime**

Add this field and methods to `MonsterRuntimeData`:

```csharp
public string[] PossibleSkillIdsByActionIndex = new string[MonsterMasterData.PossibleSkillSlotCount];

public int GetActionIndexForSkill(string skillId)
{
    if (string.IsNullOrWhiteSpace(skillId) || PossibleSkillIdsByActionIndex == null)
        return 0;

    for (int i = 0; i < PossibleSkillIdsByActionIndex.Length; i++)
    {
        if (PossibleSkillIdsByActionIndex[i] == skillId)
            return i + 1;
    }

    return 0;
}

private void InitializePossibleSkills(MonsterMasterData masterData)
{
    PossSkillIds.Clear();

    string[] slots = masterData != null
        ? masterData.GetPossibleSkillIdSlots()
        : new string[MonsterMasterData.PossibleSkillSlotCount];

    PossibleSkillIdsByActionIndex = new string[MonsterMasterData.PossibleSkillSlotCount];

    for (int i = 0; i < PossibleSkillIdsByActionIndex.Length; i++)
    {
        string skillId = i < slots.Length ? slots[i] : "";
        PossibleSkillIdsByActionIndex[i] = skillId;

        if (!string.IsNullOrWhiteSpace(skillId))
            PossSkillIds.Add(skillId);
    }
}
```

Call `InitializePossibleSkills(masterData);` at the end of the `MonsterRuntimeData(string runtimeId, MonsterMasterData masterData)` constructor after `TurnCount = 0;`.

- [ ] **Step 5: Add action index to monster reserved command**

Add this property and setter to `MonsterReservedCommand`:

```csharp
public int ActionIndex { get; private set; }

public void SetActionIndex(int actionIndex)
{
    ActionIndex = Mathf.Clamp(actionIndex, 0, MonsterMasterData.PossibleSkillSlotCount);
}
```

In the constructor, after `SkillData = skillData;`, add:

```csharp
SetActionIndex(userRuntime != null ? userRuntime.GetActionIndexForSkill(skillData != null ? skillData.SkillId : "") : 0);
```

- [ ] **Step 6: Run test to verify it passes**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: PASS with existing warnings only.

- [ ] **Step 7: Commit**

Run if `git` is available:

```bash
git add Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs Assets/Project/Scripts/Gameplay/Data/Monster/MonsterMasterData.cs Assets/Project/Scripts/Gameplay/Data/Runtime/MonsterRuntimeData.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/MonsterReservedCommand.cs
git commit -m "feat: resolve monster action index from master skill slots"
```

Expected: commit succeeds. If `git` is unavailable in the worker environment, record that and continue.

---

### Task 3: Add Player and Monster Presentation Slots

**Files:**
- Modify: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitActionPresentation.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`

- [ ] **Step 1: Add failing presentation tests**

Append these helpers and tests to `AnimationVfxLoadoutCleanupTests`:

```csharp
using System.Reflection;
using UnityEngine;

[Test]
public void BattleUnitAnimator_PlayerPowerActionSpawnsPowerVfx()
{
    GameObject owner = new("AnimatorOwner");
    BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
    GameObject powerPrefab = new("PowerVfx");

    SetPrivateField(animator, "playerPowerPresentation", new BattleUnitActionPresentation
    {
        actionStateName = "",
        actionVfx = new BattleVfxEntry { prefab = powerPrefab, flipType = VfxFlipType.None }
    });

    animator.PlaySkillAction(new SkillMasterData { SkillId = "S_Power", SkillType = SkillType.Power });

    Assert.That(owner.transform.Find("PowerVfx(Clone)"), Is.Not.Null);

    Object.DestroyImmediate(powerPrefab);
    Object.DestroyImmediate(owner);
}

[Test]
public void BattleUnitAnimator_MonsterCommandActionSpawnsMatchingActionVfx()
{
    GameObject owner = new("MonsterAnimatorOwner");
    BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
    GameObject action4Prefab = new("MonsterAction4Vfx");

    BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
    slots[3].actionStateName = "";
    slots[3].actionVfx = new BattleVfxEntry { prefab = action4Prefab, flipType = VfxFlipType.None };
    SetPrivateField(animator, "monsterActionPresentations", slots);

    MonsterMasterData master = new()
    {
        MonsterId = "M_Action4",
        HP = 10,
        PossSkillId04 = "S_Monster_Action4"
    };
    MonsterRuntimeData runtime = new("Runtime_Action4", master);
    MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Action4" });

    animator.PlayMonsterSkillAction(command);

    Assert.That(owner.transform.Find("MonsterAction4Vfx(Clone)"), Is.Not.Null);

    Object.DestroyImmediate(action4Prefab);
    Object.DestroyImmediate(owner);
}

private static void SetPrivateField(object target, string fieldName, object value)
{
    FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    Assert.That(field, Is.Not.Null, fieldName);
    field.SetValue(target, value);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL because `BattleUnitActionPresentation`, `playerPowerPresentation`, `monsterActionPresentations`, and `PlayMonsterSkillAction(MonsterReservedCommand)` do not exist yet.

- [ ] **Step 3: Create presentation slot class**

Create `BattleUnitActionPresentation.cs`:

```csharp
using System;

[Serializable]
public class BattleUnitActionPresentation
{
    public string readyStateName;
    public string actionStateName;
    public BattleVfxEntry readyVfx;
    public BattleVfxEntry actionVfx;

    public static BattleUnitActionPresentation[] CreateArray(int count)
    {
        int safeCount = UnityEngine.Mathf.Max(0, count);
        BattleUnitActionPresentation[] result = new BattleUnitActionPresentation[safeCount];

        for (int i = 0; i < result.Length; i++)
            result[i] = new BattleUnitActionPresentation();

        return result;
    }
}
```

- [ ] **Step 4: Add player Power/Skill and monster action slots to animator**

Add serialized fields to `BattleUnitAnimator`:

```csharp
[Header("Player Type Presentation")]
[SerializeField] private BattleUnitActionPresentation playerPowerPresentation = new();
[SerializeField] private BattleUnitActionPresentation playerSkillPresentation = new();

[Header("Monster Action Presentation")]
[SerializeField] private BattleUnitActionPresentation[] monsterActionPresentations =
    BattleUnitActionPresentation.CreateArray(10);
```

Add these helpers:

```csharp
private void PlayPresentationReady(BattleUnitActionPresentation presentation)
{
    if (presentation == null)
        return;

    PlayState(presentation.readyStateName);
    SpawnVfx(presentation.readyVfx);
}

private void PlayPresentationAction(BattleUnitActionPresentation presentation)
{
    if (presentation == null)
        return;

    PlayState(presentation.actionStateName);
    SpawnVfx(presentation.actionVfx);
}

private BattleUnitActionPresentation GetMonsterActionPresentation(int actionIndex)
{
    EnsureMonsterActionPresentationArray();

    int index = Mathf.Clamp(actionIndex, 1, MonsterMasterData.PossibleSkillSlotCount) - 1;
    return monsterActionPresentations[index];
}

private void EnsureMonsterActionPresentationArray()
{
    if (monsterActionPresentations != null &&
        monsterActionPresentations.Length == MonsterMasterData.PossibleSkillSlotCount)
    {
        for (int i = 0; i < monsterActionPresentations.Length; i++)
        {
            if (monsterActionPresentations[i] == null)
                monsterActionPresentations[i] = new BattleUnitActionPresentation();
        }

        return;
    }

    BattleUnitActionPresentation[] old = monsterActionPresentations;
    monsterActionPresentations =
        BattleUnitActionPresentation.CreateArray(MonsterMasterData.PossibleSkillSlotCount);

    if (old == null)
        return;

    int copyCount = Mathf.Min(old.Length, monsterActionPresentations.Length);

    for (int i = 0; i < copyCount; i++)
    {
        if (old[i] != null)
            monsterActionPresentations[i] = old[i];
    }
}
```

- [ ] **Step 5: Update player skill ready/action selection**

In `PlaySkillReady(SkillMasterData skillData)`, use:

```csharp
switch (skillData.SkillType)
{
    case SkillType.Power:
        PlayPresentationReady(playerPowerPresentation);
        break;

    case SkillType.Skill:
        PlayPresentationReady(playerSkillPresentation);
        break;

    case SkillType.Attack:
        PlayRandomAttackReady();
        break;

    default:
        PlayRandomAttackReady();
        break;
}
```

In `PlaySkillAction(SkillMasterData skillData)`, use:

```csharp
switch (skillData.SkillType)
{
    case SkillType.Power:
        PlayPresentationAction(playerPowerPresentation);
        break;

    case SkillType.Skill:
        PlayPresentationAction(playerSkillPresentation);
        break;

    case SkillType.Attack:
        PlayCurrentAttackAction();
        break;

    default:
        PlayCurrentAttackAction();
        break;
}
```

Keep the `Category.Move` branch unchanged.

- [ ] **Step 6: Add monster command animation overloads**

Add overloads:

```csharp
public void PlayMonsterSkillReady(MonsterReservedCommand command)
{
    if (command == null)
    {
        PlayIdle();
        return;
    }

    if (command.SkillData != null && command.SkillData.TimelineNotation == TimelineActionType.Move)
    {
        PlayMove();
        return;
    }

    PlayPresentationReady(GetMonsterActionPresentation(command.ActionIndex));
}

public void PlayMonsterSkillAction(MonsterReservedCommand command)
{
    if (command == null)
    {
        PlayIdle();
        return;
    }

    if (command.SkillData != null && command.SkillData.TimelineNotation == TimelineActionType.Move)
    {
        PlayMove();
        return;
    }

    PlayPresentationAction(GetMonsterActionPresentation(command.ActionIndex));
}
```

Keep the old `PlayMonsterSkillReady(MonsterSkillData skillData)` and `PlayMonsterSkillAction(MonsterSkillData skillData)` methods as compatibility wrappers that fall back to timeline notation behavior.

- [ ] **Step 7: Pass commands from monster runner animation calls**

In `BattleActionRunner`, replace calls like:

```csharp
monsterAnimator.PlayMonsterSkillReady(command.SkillData);
monsterAnimator.PlayMonsterSkillAction(command.SkillData);
```

with:

```csharp
monsterAnimator.PlayMonsterSkillReady(command);
monsterAnimator.PlayMonsterSkillAction(command);
```

Leave `PlayRandomAttackReady()` / `PlayCurrentAttackAction()` in dash attack code until that path is explicitly converted to monster action slots.

- [ ] **Step 8: Run test to verify it passes**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: PASS with existing warnings only.

- [ ] **Step 9: Commit**

Run if `git` is available:

```bash
git add Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitActionPresentation.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs
git commit -m "feat: select battle presentation from skill type and monster action index"
```

Expected: commit succeeds. If `git` is unavailable in the worker environment, record that and continue.

---

### Task 4: Move Buff/Debuff VFX to Actual Status Application

**Files:**
- Modify: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectUtility.cs`
- Test: `Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs`

- [ ] **Step 1: Add failing target-side status VFX tests**

Append these tests:

```csharp
[Test]
public void AddStatusToPlayer_SpawnsBuffVfxOnTarget()
{
    GameObject targetObject = new("PlayerTarget");
    BattleCharacter target = targetObject.AddComponent<BattleCharacter>();
    BattleUnitAnimator animator = targetObject.AddComponent<BattleUnitAnimator>();
    GameObject buffPrefab = new("BuffReceivedVfx");

    SetPrivateField(animator, "buffVfx", new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None });
    target.Initialize(new CharacterRuntimeData { CharacterId = "C_Target", MaxHP = 10, CurrentHP = 10 });

    BattleEffectUtility.AddStatusToPlayer(target, "E_Power", 1, 1);

    Assert.That(targetObject.transform.Find("BuffReceivedVfx(Clone)"), Is.Not.Null);

    Object.DestroyImmediate(buffPrefab);
    Object.DestroyImmediate(targetObject);
}

[Test]
public void AddStatusToMonster_SpawnsDebuffVfxOnTarget()
{
    GameObject targetObject = new("MonsterTarget");
    Relic.Gameplay.Monster.MonsterUnit target =
        targetObject.AddComponent<Relic.Gameplay.Monster.MonsterUnit>();
    BattleUnitAnimator animator = targetObject.AddComponent<BattleUnitAnimator>();
    GameObject debuffPrefab = new("DebuffReceivedVfx");

    SetPrivateField(animator, "debuffVfx", new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None });
    MonsterMasterData master = new() { MonsterId = "M_Target", Name = "Target", HP = 10 };
    target.Initialize(new MonsterRuntimeData("Runtime_Target", master));

    BattleEffectUtility.AddStatusToMonster(target, "E_Weaken", 1, 1);

    Assert.That(targetObject.transform.Find("DebuffReceivedVfx(Clone)"), Is.Not.Null);

    Object.DestroyImmediate(debuffPrefab);
    Object.DestroyImmediate(targetObject);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: FAIL because `AddStatusToPlayer` and `AddStatusToMonster` do not spawn target-side buff/debuff VFX.

- [ ] **Step 3: Add VFX-only methods to animator**

Add these methods to `BattleUnitAnimator`:

```csharp
public void PlayBuffVfx()
{
    SpawnVfx(buffVfx);
}

public void PlayDebuffVfx()
{
    SpawnVfx(debuffVfx);
}
```

Keep `PlayBuff()` and `PlayDebuff()` available for any existing animation callers, but do not use them for player Power/Skill presentation.

- [ ] **Step 4: Classify status effects for target VFX**

Add this enum and helpers inside `BattleEffectUtility`:

```csharp
private enum StatusVfxKind
{
    None,
    Buff,
    Debuff
}

private static StatusVfxKind GetStatusVfxKind(string effectId)
{
    switch (effectId)
    {
        case "E_Aiming":
        case "E_Armor":
        case "E_Block":
        case "E_Focus":
        case "E_Power":
        case "E_Recharge":
        case "E_Recover":
        case "E_Swift":
        case "E_Thorns":
            return StatusVfxKind.Buff;

        case "E_Addicted":
        case "E_Bleeding":
        case "E_Burn":
        case "E_Corrosion":
        case "E_Grudge":
        case "E_Vulnerable":
        case "E_Weaken":
            return StatusVfxKind.Debuff;

        default:
            return StatusVfxKind.None;
    }
}

private static void PlayStatusVfx(BattleCharacter target, string effectId)
{
    if (target == null)
        return;

    BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

    if (animator == null)
        return;

    PlayStatusVfx(animator, effectId);
}

private static void PlayStatusVfx(Relic.Gameplay.Monster.MonsterUnit target, string effectId)
{
    if (target == null)
        return;

    BattleUnitAnimator animator = target.GetComponent<BattleUnitAnimator>();

    if (animator == null)
        return;

    PlayStatusVfx(animator, effectId);
}

private static void PlayStatusVfx(BattleUnitAnimator animator, string effectId)
{
    switch (GetStatusVfxKind(effectId))
    {
        case StatusVfxKind.Buff:
            animator.PlayBuffVfx();
            break;

        case StatusVfxKind.Debuff:
            animator.PlayDebuffVfx();
            break;
    }
}
```

- [ ] **Step 5: Trigger VFX after successful status application**

In `AddStatusToPlayer`, after `AddOrStackStatus(...)`, add:

```csharp
PlayStatusVfx(target, effectId);
```

In `AddStatusToMonster`, after `AddOrStackStatus(...)`, add:

```csharp
PlayStatusVfx(target, effectId);
```

Keep `target.ShowAndRefreshHUD();` after the VFX call or immediately before it; both are acceptable as long as the status is already added.

- [ ] **Step 6: Run test to verify it passes**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: PASS with existing warnings only.

- [ ] **Step 7: Commit**

Run if `git` is available:

```bash
git add Assets/Tests/EditMode/AnimationVfxLoadoutCleanupTests.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectUtility.cs
git commit -m "feat: play status vfx when statuses are applied"
```

Expected: commit succeeds. If `git` is unavailable in the worker environment, record that and continue.

---

### Task 5: Full Verification and Cleanup

**Files:**
- Modify only files touched by Tasks 1-4 if verification finds compile or behavior issues.

- [ ] **Step 1: Search for removed loadout references**

Run:

```powershell
rg -n "CharacterSkillLoadout|CharacterRuneLoadout|MonsterSkillLoadoutData|DefaultSkillLoadout|BuildSkillLoadout|SkillLoadout|RuneLoadout" Assets\Project\Scripts Assets\Tests -S
```

Expected: no matches except historical comments in deleted files should be absent because the files are removed.

- [ ] **Step 2: Search for old monster animation calls**

Run:

```powershell
rg -n "PlayMonsterSkillReady\(command\.SkillData\)|PlayMonsterSkillAction\(command\.SkillData\)" Assets\Project\Scripts -S
```

Expected: no matches.

- [ ] **Step 3: Run MSBuild**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "RELIC.sln" /t:Build /p:Configuration=Debug /v:minimal
```

Expected: PASS with existing warnings only.

- [ ] **Step 4: Run focused Unity EditMode tests when the project is not already open**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.68f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\user\Desktop\RELIC" -runTests -testPlatform EditMode -testFilter "AnimationVfxLoadoutCleanupTests" -testResults "C:\Users\user\Desktop\RELIC\Temp\animation-vfx-loadout-results.xml" -logFile "C:\Users\user\Desktop\RELIC\Temp\animation-vfx-loadout.log" -quit
```

Expected: result XML exists and reports all tests passed. If Unity exits without XML and the log says the project is already open in another Unity instance, record the lock and keep MSBuild as the completed verification.

- [ ] **Step 5: Run broader relevant EditMode tests when Unity is available**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.68f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\user\Desktop\RELIC" -runTests -testPlatform EditMode -testFilter "AnimationVfxLoadoutCleanupTests;TimelinePreviewEntryTests;BattleActionRunnerOrderTests;PlayerMovePathfindingRegressionTests" -testResults "C:\Users\user\Desktop\RELIC\Temp\animation-vfx-regression-results.xml" -logFile "C:\Users\user\Desktop\RELIC\Temp\animation-vfx-regression.log" -quit
```

Expected: result XML exists and reports all tests passed. If Unity project lock prevents execution, record the lock.

- [ ] **Step 6: Final commit**

Run if `git` is available:

```bash
git add Assets docs/superpowers/specs/2026-06-29-animation-vfx-loadout-design.md docs/superpowers/plans/2026-06-29-animation-vfx-loadout-cleanup.md
git commit -m "chore: verify animation vfx loadout cleanup"
```

Expected: commit succeeds or reports nothing to commit if prior task commits covered all changes. If `git` is unavailable in the worker environment, record that and continue.

---

## Self-Review

Spec coverage:

- Loadout classes are removed in Task 1.
- Excel/master/runtime flow is implemented in Tasks 1 and 2.
- Monster `PossSkillId01` through `PossSkillId10` are modeled in Task 2.
- Player Power/Attack/Skill presentation rules are implemented in Task 3.
- Monster `ActionIndex` 1-10 presentation rules are implemented in Tasks 2 and 3.
- Buff/debuff VFX on actual target status application is implemented in Task 4.
- Verification is covered in Task 5.

Open item scan:

- The plan contains no unresolved markers, no unresolved code names, and no open-ended implementation steps.

Type consistency:

- `BattleUnitActionPresentation` is introduced before it is used by `BattleUnitAnimator`.
- `MonsterMasterData.PossibleSkillSlotCount` is introduced before runtime and command code use it.
- `MonsterReservedCommand.ActionIndex` is introduced before `BattleUnitAnimator.PlayMonsterSkillAction(MonsterReservedCommand)` uses it.
