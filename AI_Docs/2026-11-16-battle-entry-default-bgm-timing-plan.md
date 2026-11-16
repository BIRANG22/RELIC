# Battle Entry Default BGM Timing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 신규 배틀 씬 진입 직후 Title BGM을 Stage1Common으로 교체한다.

**Architecture:** 씬 로드 전 진행 상태를 진입 의도로 사용한다. 로드 중 Battle 씬이 생성한 첫 노드는 이어하기 판정에 영향을 주지 않는다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-16-battle-entry-default-bgm-timing-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에 작성한다.
- Unity batchmode를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

### Task 1: 씬 로드 전 진입 상태 보존

**Files:**
- Modify: `Assets/Project/Scripts/Core/GameState/States/BattleState.cs`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`

- [ ] 로드 전/후 노드 상태가 달라지는 신규 진입 테스트를 작성한다.
- [ ] 로드 전 상태만 사용하도록 정책 API와 `Enter`를 수정한다.
- [ ] 런타임 및 Editor 어셈블리 컴파일과 diff 검사를 수행한다.
