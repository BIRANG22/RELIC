# Status Effect Type Icon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Use `EffectType` from game data to choose the common status effect icon sprite.

**Architecture:** `EffectMasterData` stores the parsed type. `StatusEffectIconDatabase` resolves type icons with `EffectDatabase`. `StatusEffectIcon` keeps `IconImage` for the per-effect icon and applies the type icon to the child named `Image`.

**Tech Stack:** Unity C#, ScriptableObject databases, NUnit EditMode tests, YAML prefab/asset serialization.

## Global Constraints

- Documentation is written only under `AI_Docs`.
- Tests are written only under `Assets/Tests/EditMode~/`.
- Unity batchmode tests are not run.
- No commit, push, PR, branch, or worktree operation is performed.
- UI changes do not mutate battle core state.

---

### Task 1: Effect Type Data Contract

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Effect/EffectMasterData.cs`
- Test: `Assets/Tests/EditMode~/StatusEffectTypeIconTests.cs`

**Interfaces:**
- Produces: `EffectType` enum values `Neutral`, `Beneficial`, `Harmful`.
- Produces: `EffectMasterData.EffectType`.

- [ ] **Step 1: Write the failing test**

```csharp
[Test]
public void DataRowMapper_MapsEffectTypeColumn()
{
    var row = new Dictionary<string, string>
    {
        ["EffectId"] = "E_Boost",
        ["EffectType"] = "Beneficial"
    };

    EffectMasterData data = DataRowMapper.Map<EffectMasterData>(row);

    Assert.That(data.EffectType, Is.EqualTo(EffectType.Beneficial));
}
```

- [ ] **Step 2: Run test/build to verify it fails**

Run: `MSBuild RELIC.sln /t:Build /p:Configuration=Debug /v:minimal`

Expected: compile failure because `EffectType` does not exist.

- [ ] **Step 3: Write minimal implementation**

Add the enum and public field to `EffectMasterData`.

- [ ] **Step 4: Run verification**

Run MSBuild again and confirm the code compiles.

### Task 2: Type Icon Resolution

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/StatusEffectIconDatabase.cs`
- Modify: `Assets/Project/Scripts/UI/Battle/Canvas/StatusEffectIcon.cs`
- Test: `Assets/Tests/EditMode~/StatusEffectTypeIconTests.cs`

**Interfaces:**
- Consumes: `EffectDatabase.TryGet(string effectId, out EffectMasterData effect)`.
- Produces: `StatusEffectIconDatabase.TryGetTypeIcon(string effectId, EffectDatabase effectDatabase, out Sprite icon)`.
- Preserves: `StatusEffectIconDatabase.TryGetIcon(string effectId, out Sprite icon)` for `IconImage`.

- [ ] **Step 1: Write the failing test**

```csharp
[Test]
public void TryGetIcon_UsesCommonSpriteForEffectTypeAndNoSpriteForNeutral()
{
    StatusEffectIconDatabase iconDatabase = ScriptableObject.CreateInstance<StatusEffectIconDatabase>();
    Sprite buff = CreateSprite(Color.green);
    Sprite debuff = CreateSprite(Color.red);
    SetPrivateField(iconDatabase, "beneficialIcon", buff);
    SetPrivateField(iconDatabase, "harmfulIcon", debuff);

    EffectDatabase effectDatabase = new();
    effectDatabase.Initialize(new[]
    {
        new EffectMasterData { EffectId = "E_Buff", EffectType = EffectType.Beneficial },
        new EffectMasterData { EffectId = "E_Debuff", EffectType = EffectType.Harmful },
        new EffectMasterData { EffectId = "E_Neutral", EffectType = EffectType.Neutral },
    });

    Assert.That(iconDatabase.TryGetTypeIcon("E_Buff", effectDatabase, out Sprite buffResult), Is.True);
    Assert.That(buffResult, Is.SameAs(buff));
    Assert.That(iconDatabase.TryGetTypeIcon("E_Debuff", effectDatabase, out Sprite debuffResult), Is.True);
    Assert.That(debuffResult, Is.SameAs(debuff));
    Assert.That(iconDatabase.TryGetTypeIcon("E_Neutral", effectDatabase, out Sprite neutralResult), Is.False);
    Assert.That(neutralResult, Is.Null);
}
```

- [ ] **Step 2: Run test/build to verify it fails**

Run: `MSBuild RELIC.sln /t:Build /p:Configuration=Debug /v:minimal`

Expected: compile failure because the overload does not exist.

- [ ] **Step 3: Write minimal implementation**

Add serialized `beneficialIcon` and `harmfulIcon`, implement type-based icon lookup, and update `StatusEffectIcon` so `IconImage` uses per-effect icons while the child `Image` uses the type icon.

- [ ] **Step 4: Run verification**

Run MSBuild again and confirm the code compiles.

### Task 3: Asset Wiring

**Files:**
- Modify: `Assets/DB/StatusEffectIconDatabase.asset`
- Modify: `Assets/Project/PrefabsR/HUD_Prefab/StatusEffectIcon.prefab`

**Interfaces:**
- Consumes: `beneficialIcon` and `harmfulIcon` serialized fields.

- [ ] **Step 1: Connect sprites**

Set:

```yaml
beneficialIcon: {fileID: 21300000, guid: 8baaaf469a664b54089db57f0e77dfcc, type: 3}
harmfulIcon: {fileID: 21300000, guid: b98f9d4577eba5240a6261d95dd2e229, type: 3}
```

Set `StatusEffectIcon.typeIconImage` on the prefab to the child object named `Image`, not `IconImage`.

- [ ] **Step 2: Verify asset references**

Run a text check that the two GUIDs exist in `StatusEffectIconDatabase.asset`.

- [ ] **Step 3: Final verification**

Run MSBuild, `git diff --check`, and inspect the final diff.
