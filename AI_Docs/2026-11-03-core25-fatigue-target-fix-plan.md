# Core25 Fatigue Target Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `S_Core_25`의 쇠약 3이 시전자가 아닌 범위 내 몬스터에게 적용되도록 수정한다.

**Architecture:** `FatigueEffect`가 범용 기본 대상 추론을 사용하지 않고 `PlayerTarget`과 `MonsterTarget`을 명시적으로 선택한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-03-core25-fatigue-target-fix-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR, 브랜치/worktree 작업을 수행하지 않는다.

## Review Focus

- `PlayerCaster`와 `MonsterTarget`이 동시에 있을 때 몬스터만 상태를 받는가.
- 플레이어 대상 쇠약 효과는 계속 `PlayerTarget`에 적용되는가.
- `PlayerTarget`과 `MonsterTarget`이 모두 없을 때 시전자에게 잘못 적용되지 않는가.
- 값과 반복 횟수의 기존 스택 계산이 유지되는가.
- 다른 디버프 효과와 범용 대상 선택 함수에는 영향이 없는가.

---

### Task 1: FatigueEffect 명시적 대상 선택

**Files:**
- Modify: `Assets/Project/Scripts/FatigueEffect.cs`
- Test: `Assets/Tests/EditMode~/FatigueEffectTargetingTests.cs`

**Interfaces:**
- Consumes: `BattleEffectContext.PlayerTarget`, `BattleEffectContext.MonsterTarget`
- Produces: 명시적 쇠약 대상 적용 규칙

- [ ] 플레이어 시전자와 몬스터 대상이 함께 있는 실패 테스트를 작성한다.
- [ ] 플레이어 대상 적용과 대상 없는 컨텍스트 테스트를 작성한다.
- [ ] 기존 구현에서 테스트가 실패하는지 확인한다.
- [ ] `FatigueEffect.Apply()`를 명시적 대상 분기로 변경한다.
- [ ] 테스트 코드와 런타임·에디터 프로젝트를 컴파일한다.
- [ ] 변경 diff와 멀티플레이 경계를 검토한다.
