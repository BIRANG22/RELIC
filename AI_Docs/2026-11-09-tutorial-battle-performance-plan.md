# Tutorial Battle Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 튜토리얼 전투 UI의 매 프레임 전체 탐색·강제 레이아웃 갱신·배열 할당을 제거한다.

**Architecture:** 변경 감지 정책을 순수 함수로 분리하고 기존 프레젠테이션 코드가 해당 정책을 사용한다. 데이터 기반 튜토리얼이 존재하면 구형 컨트롤러는 실행되지 않는다.

**Tech Stack:** Unity, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-09-tutorial-battle-performance-design.md`

## Global Constraints

- 전투 규칙과 튜토리얼 진행 순서를 변경하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 커밋, 브랜치, worktree는 생성하지 않는다.

## Review Focus

- 첫 프레임에는 버튼 및 툴팁 갱신이 반드시 수행되어야 한다.
- 같은 단계와 같은 해상도에서는 반복 갱신하지 않아야 한다.
- 단계 또는 해상도가 바뀌면 즉시 다시 갱신해야 한다.
- 데이터 기반 튜토리얼 맵에서 레거시 컨트롤러가 반복 실행되지 않아야 한다.
- 일반 전투 및 데이터가 없는 레거시 경로가 유지되어야 한다.

---

### Task 1: 변경 감지 및 DB 조회 정책

**Files:**
- Modify: `Assets/Project/Scripts/TutorialDatabase.cs`
- Test: `Assets/Tests/EditMode~/TutorialBattlePerformanceTests.cs`

- [ ] 실패 테스트를 작성한다.
- [ ] 컴파일하여 RED를 확인한다.
- [ ] 단계/해상도 변경 정책과 무할당 `HasForMap`을 구현한다.
- [ ] 컴파일하여 GREEN을 확인한다.

### Task 2: 튜토리얼 프레젠테이션 갱신 최적화

**Files:**
- Modify: `Assets/Project/Scripts/TutorialBattleSequence.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Tutorial/BattleFirstTutorialController.cs`
- Test: `Assets/Tests/EditMode~/TutorialBattlePerformanceTests.cs`

- [ ] 소스 연결 검증 테스트를 먼저 추가한다.
- [ ] RED를 확인한다.
- [ ] 버튼 갱신을 단계 변경 시점으로 제한한다.
- [ ] 툴팁 재보정을 표시/해상도 변경 시점으로 제한한다.
- [ ] 데이터 기반 맵에서 레거시 컨트롤러를 비활성화한다.
- [ ] 전체 컴파일과 관련 테스트를 검증한다.
