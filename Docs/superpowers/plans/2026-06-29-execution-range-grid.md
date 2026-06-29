# Execution Range Grid Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the current battle action's range on the base grid during execution, then hide it again after the action ends.

**Architecture:** Add base-renderer execution tint APIs to `GridCell` and `GridManager`, then call them from `BattleActionRunner` around each action routine. Keep existing `RangePreview` highlight-object behavior unchanged for reservation and hover previews.

**Tech Stack:** Unity C#, NUnit EditMode tests, MaterialPropertyBlock renderer tinting.

---

### Task 1: Base Grid Execution Range API

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/GridCell.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Runtime/Battle/GridManager.cs`
- Test: `Assets/Tests/EditMode~/ExecutionRangeGridTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Test]
public void ShowExecutionRange_EnablesAndTintsOnlyRequestedBaseGridCells()
{
    GridManager gridManager = CreateTwoCellGrid(out GameObject root, out Renderer first, out Renderer second);

    try
    {
        gridManager.SetGridVisible(false);
        gridManager.ShowExecutionRange(new[] { 0 }, Color.red);

        Assert.That(first.enabled, Is.True);
        Assert.That(second.enabled, Is.False);
        Assert.That(ReadRendererColor(first), Is.EqualTo(Color.red).Using(ColorComparer));

        gridManager.ClearExecutionRange();

        Assert.That(first.enabled, Is.False);
        Assert.That(second.enabled, Is.False);
    }
    finally
    {
        Object.DestroyImmediate(root);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity EditMode test filter `ExecutionRangeGridTests.ShowExecutionRange_EnablesAndTintsOnlyRequestedBaseGridCells`

Expected: FAIL because `GridManager.ShowExecutionRange` does not exist.

- [ ] **Step 3: Write minimal implementation**

Add `GridCell.SetExecutionRangeTint(Color)` and `GridCell.ClearExecutionRangeTint()` for base renderers, then add `GridManager.ShowExecutionRange(IEnumerable<int>, Color)` and `GridManager.ClearExecutionRange()`.

- [ ] **Step 4: Run test to verify it passes**

Run the same Unity EditMode test filter.

Expected: PASS.

### Task 2: Action Runner Range Resolution and Wiring

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/ExecutionRangeGridTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Test]
public void BuildPlayerExecutionRange_UsesReservedMoveRangeForMoveCommand()
{
    PlayerReservedCommand command = CreatePlayerMoveCommand(12, new List<int> { 8, 12 });
    List<int> range = InvokeBuildPlayerExecutionRange(command);
    Assert.That(range, Is.EqualTo(new List<int> { 8, 12 }));
}

[Test]
public void BuildMonsterMoveExecutionRange_UsesMovedOccupiedCells()
{
    GridManager gridManager = new GameObject("Grid").AddComponent<GridManager>();
    MonsterUnit monster = CreateMonsterAt(new List<int> { 0, 1 });
    MonsterReservedCommand command = CreateMonsterMoveCommand(Vector2Int.right);

    List<int> range = InvokeBuildMonsterMoveExecutionRange(gridManager, monster, command);

    Assert.That(range, Is.EqualTo(new List<int> { 5, 6 }));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity EditMode test filter `ExecutionRangeGridTests`

Expected: FAIL because the private range-resolution helpers do not exist.

- [ ] **Step 3: Write minimal implementation**

Add private helpers in `BattleActionRunner` for player command and monster move execution range. Wrap each player and monster action routine with `ShowExecutionRange` and `ClearExecutionRange` calls.

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity EditMode test filter `ExecutionRangeGridTests`

Expected: PASS.

### Task 3: Verification

**Files:**
- Verify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/GridCell.cs`
- Verify: `Assets/Project/Scripts/Gameplay/Runtime/Battle/GridManager.cs`
- Verify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`

- [ ] **Step 1: Run focused tests**

Run: Unity EditMode filter `ExecutionRangeGridTests`

Expected: all tests pass.

- [ ] **Step 2: Run relevant regression tests**

Run: Unity EditMode filter `BattleActionRunnerOrderTests;MonsterMoveRegressionTests;PlayerMovePathfindingRegressionTests`

Expected: all tests pass.

- [ ] **Step 3: Build**

Run: MSBuild Debug.

Expected: build succeeds without new compile errors.
