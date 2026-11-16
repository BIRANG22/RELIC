# Runtime Frame Cost Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 블러 UI, persistent idle VFX, 튜토리얼 포커스의 불필요한 매 프레임 검색과 동기화를 제거한다.

**Architecture:** 기존 MonoBehaviour와 프리팹 연결을 유지하고 각 영역에 작은 dirty/snapshot/cache 정책을 추가한다. 전투 핵심 상태는 건드리지 않고 presentation 계층에서 상태가 바뀐 경우에만 기존 갱신 코드를 실행한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-16-runtime-frame-cost-optimization-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 추가한다.
- Unity batchmode를 사용하지 않는다.
- 전투 Command, State Change, Result/Event 및 네트워크 snapshot 형식을 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 블러 중 동적 텍스트·색상·활성 상태 변경이 다음 프레임에 replica에 반영되는가.
- 동적 UI 자식 추가·삭제 후 replica가 안전하게 재구성되는가.
- 몬스터 페이즈·방향·sorting order 변경 시 persistent idle VFX가 즉시 따라가는가.
- idle VFX suspend/resume과 사망 정리가 기존대로 동작하는가.
- 튜토리얼 타깃 자식이 동적으로 교체될 때 오래된 캐시를 사용하지 않는가.

---

### Task 1: UI 블러 계층 및 CanvasGroup 캐시

**Files:**
- Modify: `Assets/Project/Scripts/UIBlurReplicaSource.cs`
- Modify: `Assets/Project/Scripts/UIBlurBackgroundManager.cs`
- Test: `Assets/Tests/EditMode~/UIBlurReplicaDirtyStateTests.cs`

- [ ] 중첩 자식 추가·교체 시 replica 재구성이 유지되는 회귀 테스트를 작성한다.
- [ ] 기존 동작을 보존하는 테스트 조건을 확인한다.
- [ ] 계층 topology와 CanvasGroup 부모 목록 캐시를 구현한다.
- [ ] 런타임 및 Editor 어셈블리 컴파일로 GREEN을 확인한다.

### Task 2: Persistent Idle VFX 상태 캐시

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`
- Test: `Assets/Tests/EditMode~/BattleUnitAnimatorPersistentIdleVfxTests.cs`

- [ ] 동일 snapshot과 sorting 상태에서 갱신이 불필요함을 검증하는 실패 테스트를 작성한다.
- [ ] Editor 테스트 어셈블리 컴파일 실패로 RED를 확인한다.
- [ ] snapshot 비교와 컴포넌트/sorting 캐시를 구현한다.
- [ ] 런타임 및 Editor 어셈블리 컴파일로 GREEN을 확인한다.

### Task 3: 튜토리얼 포커스 자식 캐시

**Files:**
- Modify: `Assets/Project/Scripts/TutorialFocusGuideRunner.cs`
- Test: `Assets/Tests/EditMode~/TutorialBattlePerformanceTests.cs`

- [ ] 같은 타깃과 계층에서는 캐시를 재사용하고 계층 변경 시 무효화하는 실패 테스트를 작성한다.
- [ ] Editor 테스트 어셈블리 컴파일 실패로 RED를 확인한다.
- [ ] 캐시 정책과 runner 연결을 구현한다.
- [ ] 전체 런타임 및 Editor 어셈블리 컴파일과 diff 검사를 수행한다.
