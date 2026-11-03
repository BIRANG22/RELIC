# Arabella Deferred Phase Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아라벨라 1페이즈 사망 시 해당 개체의 남은 예약만 제거하고 턴 행동 종료 후 2페이즈로 전환한다.

**Architecture:** `RuntimeId` 기반 명령 제거 정책을 순수 로직으로 검증하고 타임라인 저장소와 실행 배치에 동일하게 적용한다. 페이즈 전환 상태는 즉시 예약하되 연출과 런타임 교체는 타임라인 행동 종료 경계에서 수행한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Animator Controller YAML

**Spec:** `AI_Docs/2026-11-03-arabella-deferred-phase-transition-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 전투 대상 식별은 `RuntimeId`를 사용한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 사망한 현재 슬롯은 재인덱싱하거나 소급 취소하지 않는다.
- 다른 몬스터와 플레이어 행동을 제거하지 않는다.
- 2페이즈 행동을 현재 턴에 새로 예약하지 않는다.
- 턴 종료 효과로 1페이즈가 죽은 경우 중복 전환하지 않는다.
- 전환 대기 표현이 2페이즈 최종 Dead 재생 잠금을 소비하지 않는다.

---

### Task 1: RuntimeId 기반 예약 제거 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaPhaseTransition.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleActionBatch.cs`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionTests.cs`

- [ ] 실패 테스트로 동일 RuntimeId 명령만 제거되고 다른 명령은 유지되는지 고정한다.
- [ ] 순수 명령 제거 API를 구현한다.
- [ ] 테스트 코드 컴파일을 검증한다.

### Task 2: 타임라인과 턴 실행 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`

- [ ] 타임라인 저장소에서 RuntimeId별 몬스터 예약을 제거하고 UI를 갱신한다.
- [ ] 전역 Skip을 제거하고 전환 감지 시 남은 배치의 아라벨라 명령만 제거한다.
- [ ] 타임라인 행동 종료 뒤 대기 중인 전환을 완료한다.

### Task 3: 1페이즈 사망 대기 표현

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Controller/Arabella_BattleAnimatorController.controller`
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionTests.cs`

- [ ] 전환 예약 시 `PhaseOneDefeated` 상태를 재생하는 API를 추가한다.
- [ ] Animator Controller 상태에 반복 가능한 `Idle` Motion을 임시 연결한다.
- [ ] YAML 참조와 C# 컴파일을 검증한다.

### Task 4: 통합 검증

- [ ] 관련 테스트 코드를 컴파일한다.
- [ ] `Assembly-CSharp`와 `Assembly-CSharp-Editor`를 컴파일한다.
- [ ] 변경 파일의 `git diff --check`를 실행한다.
- [ ] 독립 코드 리뷰에서 전체 턴 유지와 대상 한정 제거를 확인한다.
