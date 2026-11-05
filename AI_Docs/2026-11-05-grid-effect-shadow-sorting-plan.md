# Grid Effect Shadow Sorting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아래쪽 셀의 효과 그리드 오브젝트가 유닛 그림자보다 위에 그려지도록 전투 Y-sort를 일치시킨다.

**Architecture:** 효과 그리드를 `Unit` Sorting Layer로 통일하고 셀 월드 Y에서 유닛 기준 sorting order를 계산한다. 효과는 같은 셀의 유닛과 그림자 사이 order를 사용하며 프리팹 renderer의 상대 order를 보존한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity scene YAML

**Spec:** `AI_Docs/2026-11-05-grid-effect-shadow-sorting-design.md`

## Global Constraints

- 문서는 `AI_Docs`, 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 수행하지 않는다.
- 전투 상태와 판정 로직은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 같은 셀의 유닛·효과·그림자 순서가 각각 0, -1, -2 상대값을 유지한다.
- 아래쪽 셀일수록 더 높은 sorting order를 얻는다.
- 음수·소수 Y에서도 기존 정수 변환 규칙과 일치한다.
- 프리팹 renderer별 상대 order를 잃지 않는다.
- 미리보기와 실제 생성 효과가 동일한 정렬 경로를 사용한다.

---

### Task 1: 효과 그리드 Y-sort 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleUnitHighlightSortingPolicy.cs`
- Create: `Assets/Tests/EditMode~/BattleGridEffectSortingPolicyTests.cs`

**Interfaces:**
- Consumes: 셀 월드 Y, Y 배율, 기준 오프셋, 프리팹 renderer order
- Produces: `BattleGridEffectSortingPolicy.ResolveOrder(float, float, int, int)`

- [ ] **Step 1: Write failing tests for same-cell layering, lower-cell precedence, and prefab relative order**
- [ ] **Step 2: Compile the test and confirm it fails because the policy does not exist**
- [ ] **Step 3: Implement the minimal pure sorting policy**
- [ ] **Step 4: Compile the test with the runtime and Editor assemblies**

### Task 2: 효과 그리드 renderer 적용

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/BattleGridEffectController.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: Task 1의 `ResolveOrder`
- Produces: 실제 효과와 미리보기 renderer의 `Unit` 레이어 및 셀 기반 order

- [ ] **Step 1: Pass cell world Y into both renderer sorting call sites**
- [ ] **Step 2: Preserve prefab relative order while applying the policy**
- [ ] **Step 3: Enable `Unit` sorting-layer override in the battle scene**
- [ ] **Step 4: Verify attack-direction tests, source consistency, and both Unity C# projects compile**

