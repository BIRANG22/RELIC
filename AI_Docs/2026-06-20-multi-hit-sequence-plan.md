# Multi-Hit Skill Sequence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make attack skill `Count` execute as repeated hit sequences instead of immediate stacked damage.

**Architecture:** Keep damage math inside effect classes, but move multi-hit presentation sequencing into the battle action execution layer. Damage effects execute one hit per call; runner/service code repeats `attack -> effect -> camera impact` for `Count`.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing battle action runner/effect system.

---

## File Structure
- Modify `Assets/Tests/EditMode/BattleActionRegressionTests.cs`: add regression tests for single-hit effect execution and multi-hit helper behavior.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/StrikeEffect.cs`: remove internal `Count` damage loop.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/PierceEffect.cs`: remove internal `Count` damage loop.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`: run player attack effects as hit-by-hit coroutine sequences.
- Modify `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`: expose per-target hit application so monster runner can sequence multi-hit attacks.

### Task 1: Lock Damage Effects To One Hit Per Execute

**Files:**
- Test: `Assets/Tests/EditMode/BattleActionRegressionTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/StrikeEffect.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/PierceEffect.cs`

- [ ] **Step 1: Write failing tests**

Add tests named:
- `StrikeEffect_ExecuteAppliesOnlyOneHitEvenWhenContextCountIsThree`
- `PierceEffect_ExecuteAppliesOnlyOneHitEvenWhenContextCountIsThree`

Each test creates a target with 30 HP, executes the effect with `Value = 5`, `Count = 3`, and expects HP to become 25.

- [ ] **Step 2: Run tests to verify failure**

Run Unity EditMode tests for `BattleActionRegressionTests`.
Expected before implementation: both tests fail because HP becomes 15.

- [ ] **Step 3: Remove internal damage loops**

Change `StrikeEffect.Apply` and `PierceEffect.Apply` so each `Execute` call applies one damage hit to the selected target. Keep existing damage modifiers and death checks.

- [ ] **Step 4: Run tests to verify pass**

Run the same EditMode tests.
Expected after implementation: both new tests pass.

### Task 2: Add Player Multi-Hit Sequencing

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`

- [ ] **Step 1: Add focused helper methods**

Add helpers that detect attack damage effects, clamp hit count, clone a `BattleEffectContext` with `Count = 1`, and check whether the target is dead.

- [ ] **Step 2: Convert player skill effect execution to coroutine**

Change player skill effect execution from immediate `void` calls to `IEnumerator` calls. For `E_Strike` and `E_Pierce`, loop `Count` times:

```text
PlaySkillAction
Wait ActionDelay
Execute effect once with Count = 1
PlayDamageImpact if target exists
Wait HitCameraDelay
Stop if target died
```

- [ ] **Step 3: Preserve non-damage effects**

Non-damage effects continue to execute once with their existing `Count` value, so status duration and stack semantics do not change.

### Task 3: Add Monster Multi-Hit Sequencing

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/MonsterSkillEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`

- [ ] **Step 1: Split monster effect targeting from execution**

Expose a method that returns or applies one target/effect hit at a time, without owning camera timing.

- [ ] **Step 2: Sequence monster attack hits in `ExecuteMonsterSkill`**

For attack damage effects, run the same `attack -> one damage effect -> camera impact` loop that player skills use.

- [ ] **Step 3: Keep buff/debuff monster skills immediate**

Monster buff/debuff effects keep current single execution behavior.

### Task 4: Verification

**Files:**
- No new files.

- [ ] **Step 1: Run focused tests**

Run Unity EditMode tests for `BattleActionRegressionTests`.

- [ ] **Step 2: Run compile verification**

Run the available Unity/solution compile check if available in this workspace.

- [ ] **Step 3: Manual Unity check**

In battle, use a `Count = 3` attack and confirm visible order:

```text
attack, hit/camera, attack, hit/camera, attack, hit/camera
```

Stop early on target death.
