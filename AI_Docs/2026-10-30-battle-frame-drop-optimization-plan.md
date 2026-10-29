# Battle Frame Drop Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 UI의 매 프레임 전체 검색과 재귀 계층 검색을 제거해 저사양 환경의 CPU 부하를 줄인다.

**Architecture:** 전투 상태와 결과 데이터는 그대로 두고 표시 계층에 작은 캐시와 순수 갱신 조건을 추가한다. 캐릭터 하이라이트는 프레임 상태를 한 번 계산하고, 타임라인은 최초 조회한 Transform/TMP_Text를 재사용한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-30-battle-frame-drop-optimization-design.md`

## Global Constraints

- 전투 결과 로직과 네트워크 명령 형식을 변경하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 추가한다.
- Unity batchmode는 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 하이라이트가 숨겨진 상태에서 애니메이터가 갱신되지 않는가.
- 선택 또는 호버 상태에서는 기존 애니메이션 동기화가 유지되는가.
- 튜토리얼/전투 실행/전투 종료 시 하이라이트가 즉시 숨는가.
- 타임라인 UI 재구성 후 오래된 Transform 캐시가 남지 않는가.
- 캐시 대상이 파괴된 경우 안전하게 다시 조회하는가.

---

### Task 1: 캐릭터 하이라이트 갱신 조건 및 참조 캐시

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleCharacterHighlightFrameState.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleCharacter.cs`
- Test: `Assets/Tests/EditMode~/BattleCharacterHighlightFrameStateTests.cs`

- [x] 하이라이트가 보이지 않을 때 애니메이터 동기화를 요구하지 않는 실패 테스트를 작성한다.
- [x] 테스트 컴파일 실패를 확인한다.
- [x] 순수 프레임 상태 타입과 `ShouldSynchronizeAnimator` 조건을 구현한다.
- [x] `BattleCharacter`가 프레임 상태를 한 번 계산하고 검색한 컴포넌트를 캐시하도록 연결한다.
- [x] 테스트 및 런타임 컴파일을 확인한다.

### Task 2: 타임라인 계층 조회 캐시

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleTimelineTransformCache.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineTransformCacheTests.cs`

- [x] 최초 조회한 Transform/TMP_Text를 계층 이름 변경 뒤에도 반환하는 실패 테스트를 작성한다.
- [x] 테스트 컴파일 실패를 확인한다.
- [x] 파괴된 참조를 재조회하고 명시적으로 초기화 가능한 캐시를 구현한다.
- [x] `FindSizeRoot`와 `FindUseSkillText`에 캐시를 연결하고 UI 초기화 시 비운다.
- [x] 테스트 및 전체 컴파일, diff 검사를 수행한다.
