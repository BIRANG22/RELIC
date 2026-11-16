# Resume Room BGM Immediate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 이어하기 시 기본 전투 BGM을 거치지 않고 현재 방 BGM이 유지되게 한다.

**Architecture:** `BattleState`는 신규 진입의 기본 BGM만 담당한다. 미완료 방 이어하기의 BGM은 기존 `BattleBgmDirector`가 선택하며, `BattleState`가 뒤늦게 덮어쓰지 않는다.

**Tech Stack:** Unity, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-16-resume-room-bgm-immediate-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 및 에디터 직접 제어를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 신규 탐사 진입에서 기본 BGM이 사라지지 않아야 한다.
- 미완료 Common 방 이어하기에서도 불필요한 기본 BGM 재요청이 없어야 한다.
- 미완료 Boss 방의 `BattleBgmDirector` 선택을 `BattleState`가 덮어쓰지 않아야 한다.
- 방 BGM 매핑과 우선순위에는 변화가 없어야 한다.
- 전투 상태 및 멀티플레이 동기화 데이터에는 변화가 없어야 한다.

---

### Task 1: 이어하기 기본 BGM 요청 차단

**Files:**
- Modify: `Assets/Tests/EditMode~/ContextualBgmTests.cs`
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`
- Modify: `Assets/Project/Scripts/Core/GameState/States/BattleState.cs`

**Interfaces:**
- Consumes: `MapRuntimeProgressUtility.HasUnclearedCurrentNode(MapRuntimeData)`
- Produces: `BattleState.ShouldPlayDefaultBattleBgm(bool)`의 신규 진입 전용 정책

- [x] **Step 1:** 미완료 노드에서는 기본 BGM을 재생하지 않는 실패 테스트로 기대값을 수정한다.
- [x] **Step 2:** 현재 구현이 미완료 노드에서도 `true`를 반환해 테스트를 실패시키는지 확인한다.
- [x] **Step 3:** `ShouldPlayDefaultBattleBgm`이 `!hasUnclearedCurrentNode`를 반환하도록 최소 수정한다.
- [x] **Step 4:** 관련 테스트와 런타임/에디터 C# 컴파일을 검증한다.
