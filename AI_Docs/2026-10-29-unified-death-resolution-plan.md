# 피해 원인 공통 사망 처리 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어와 몬스터의 치명 피해가 피해 원인과 무관하게 각 유닛 유형의 공통 사망 처리 진입점을 사용하게 한다.

**Architecture:** 플레이어는 새 `BattlePlayerIncapacitationService`가 생존→HP 0 전환과 런타임 정리를 담당한다. 몬스터는 기존 `BattleDeathService`만 사용하며 그리드 컨트롤러의 임시 사망 서비스를 제거한다. 피해 계산과 표현은 분리하고 아라벨라 페이즈 전환을 Dead 연출보다 우선한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-29-unified-death-resolution-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 하지 않는다.
- UI/VFX는 사망 결과를 계산하지 않고 확정된 결과만 재생한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 이미 전투불능인 플레이어를 다시 처리하거나 침식도를 중복 증가시키지 않는다.
- 보호막 피해만 받은 플레이어를 전투불능으로 처리하지 않는다.
- 그리드 피해 통계에 보호막과 HP 실제 감소량이 모두 기록된다.
- 아라벨라 1페이즈는 Dead 연출을 재생하지 않고 2페이즈는 정상 사망 연출 대상이다.
- 그리드 사망 몬스터가 정상 `BattleDeathService`에서 한 번만 처리된다.

---

### Task 1: 플레이어 공통 전투불능 확정

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattlePlayerIncapacitationService.cs`
- Create: `Assets/Tests/EditMode~/BattlePlayerIncapacitationServiceTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectUtility.cs`
- Modify: `Assets/Project/Scripts/BattleErosionRuntimeService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleGridEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/BattleGridEffectController.cs`

**Interfaces:**
- Produces: `BattlePlayerIncapacitationService.ResolveState(CharacterRuntimeData runtime, int hpBefore) -> bool`, `Resolve(BattleCharacter character, int hpBefore) -> bool`

- [ ] **Step 1: 실패 회귀 테스트 작성**
- [ ] **Step 2: RED 경로 확인 및 테스트 코드 컴파일**
- [ ] **Step 3: 공통 서비스 구현과 모든 플레이어 피해 경로 연결**
- [ ] **Step 4: 그리드 피해 통계·이벤트·팝업 준비 연결**
- [ ] **Step 5: 런타임 및 Editor 프로젝트 컴파일**

### Task 2: 몬스터 그리드 사망 단일 진입점

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/BattleGridEffectController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleDeathService.cs`
- Create: `Assets/Tests/EditMode~/GridEffectDeathResolutionPolicyTests.cs`

**Interfaces:**
- Consumes: 기존 `BattleDeathService.HandleMonsterDead(MonsterUnit)`
- Produces: `BattleGridEffectController.ShouldPlayDeadPresentation(MonsterRuntimeData) -> bool`

- [ ] **Step 1: 아라벨라·일반 몬스터 표현 정책 실패 테스트 작성**
- [ ] **Step 2: 그리드 컨트롤러의 임시 사망 서비스 제거**
- [ ] **Step 3: 실제 ActionRunner/DeathService 문맥에서 정상 사망 서비스 호출**
- [ ] **Step 4: 아라벨라 페이즈 전환 우선 표현 정책 구현**
- [ ] **Step 5: 전체 변경 diff 및 컴파일 검증**
