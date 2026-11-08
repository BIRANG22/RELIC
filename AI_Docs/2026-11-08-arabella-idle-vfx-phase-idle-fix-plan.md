# Arabella Idle VFX and Phase Idle Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아라벨라 지속 IdleVFX가 방향을 따르고 점프 중 남지 않게 하며, 2페이즈 전환 직전 1페이즈 Idle 노출을 제거한다.

**Architecture:** `BattleUnitAnimator`가 지속 VFX의 방향·일시 정지 상태를 소유한다. `BattleActionRunner`는 점프 구간에서 표시만 일시 정지하며, Animator Controller 데이터는 패배 상태에 올바른 클립을 연결한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity Animator Controller YAML

**Spec:** `AI_Docs/2026-11-08-arabella-idle-vfx-phase-idle-fix-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- VFX는 전투 상태를 계산하거나 변경하지 않는다.
- Unity 에디터 직접 제어 및 batchmode 테스트는 하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 같은 방향에서는 매 프레임 VFX가 재생성되지 않아야 한다.
- 방향 변경 시 현재 페이즈 VFX가 정확히 한 번 갱신되어야 한다.
- 중첩 정지와 코루틴 중단 후에도 VFX가 올바르게 복구되어야 한다.
- 사망 상태에서는 복구가 VFX를 다시 생성하지 않아야 한다.
- `PhaseOneDefeated`만 `Dead.anim`으로 바뀌고 기본 `Idle`은 유지되어야 한다.

---

### Task 1: 지속 IdleVFX 방향 및 일시 정지

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Test: `Assets/Tests/EditMode~/BattleUnitAnimatorPersistentIdleVfxTests.cs`

**Interfaces:**
- Produces: `void SuspendPersistentIdleVfx()`
- Produces: `void ResumePersistentIdleVfx()`

- [x] 방향 변경과 중첩 정지/복구의 실패 테스트를 작성한다.
- [x] 생성 방향을 기억하고 달라졌을 때만 지속 VFX를 갱신한다.
- [x] 정지 카운터가 1 이상이면 VFX를 제거하고 재생성을 막는다.
- [x] 카운터가 0이 될 때 현재 페이즈 VFX를 복구한다.

### Task 2: 사냥 강습 점프 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/ArabellaHuntingLeapIdleVfxTests.cs`

**Interfaces:**
- Consumes: `SuspendPersistentIdleVfx()` / `ResumePersistentIdleVfx()`

- [x] 점프 루틴이 정지와 복구를 보장하는 실패 테스트를 작성한다.
- [x] `AnimateArabellaHuntingLeap` 실행 구간을 `try/finally`로 감싸 중단 시에도 복구한다.

### Task 3: PhaseOneDefeated 모션 교체

**Files:**
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Controller/Arabella_BattleAnimatorController.controller`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionTests.cs`

- [x] `PhaseOneDefeated`가 `Dead.anim`, 기본 `Idle`이 `Idle.anim`을 참조하는 에셋 검증 테스트를 작성한다.
- [x] Controller의 `PhaseOneDefeated` 모션 GUID만 `Dead.anim` GUID로 교체한다.

### Task 4: 검증

- [x] 런타임 어셈블리를 컴파일한다.
- [x] 새 테스트를 포함해 에디터 어셈블리를 컴파일한다.
- [x] 변경 diff와 프리팹 직렬화 호환성을 검토한다.
