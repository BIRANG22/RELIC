# Ward Reaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 직접 공격 피해를 받은 `E_Ward` 보유자가 공격자에게 스택만큼 반사 피해를 주도록 구현한다.

**Architecture:** 순수 계산과 런타임 적용을 `BattleWardReactionService`로 분리하고, 실제 직접 피해량이 확정되는 `StrikeEffect`와 `PierceEffect`에서 호출한다. 반사 피해는 기존 피해 API를 사용하되 직접 공격 효과를 다시 실행하지 않아 연쇄 반사를 차단한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-04-ward-reaction-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity 에디터 및 batchmode 테스트를 실행하지 않는다.
- UI/VFX/사운드는 전투 결과 계산에 관여하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 실제 피해가 0이면 반사가 발생하지 않아야 한다.
- 중복된 `E_Ward` 런타임 항목은 유효한 양수 스택을 합산해야 한다.
- 직접 공격이 아닌 피해 경로에서는 서비스를 호출하지 않아야 한다.
- 반사 피해가 또 다른 반사를 발생시키지 않아야 한다.
- 플레이어와 몬스터 어느 쪽이 공격자여도 기존 사망 처리 경로가 유지되어야 한다.

---

### Task 1: 가시 반응 계산 및 적용

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleWardReactionService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/StrikeEffect.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/PierceEffect.cs`
- Test: `Assets/Tests/EditMode~/BattleWardReactionServiceTests.cs`

**Interfaces:**
- Consumes: `BattleEffectContext`, `CharacterRuntimeData.StatusEffects`, `MonsterRuntimeData.StatusEffects`, `BattleEffectUtility`
- Produces: `BattleWardReactionService.ResolveReactionDamage(...)`, `BattleWardReactionService.Apply(...)`

- [ ] **Step 1: 실패하는 순수 계산 테스트 작성**
- [ ] **Step 2: 컴파일로 새 API 부재에 따른 RED 확인**
- [ ] **Step 3: 최소 계산 및 적용 서비스 구현**
- [ ] **Step 4: Strike/Pierce의 실제 피해 확정 뒤 서비스 호출**
- [ ] **Step 5: 런타임/에디터 어셈블리 컴파일 및 정적 호출 경로 검증**
