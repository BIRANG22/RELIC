# Monster Execution-Time Facing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 일반 예약 행동의 몬스터 플립을 턴 시작 전이 아니라 실제 행동 실행 직전에 적용한다.

**Architecture:** `BattleMonsterTurnPlanner`는 방향을 투영해 `FlipBeforeAction`만 기록하고 실제 몬스터 상태는 바꾸지 않는다. 일반 선행 방향 호출은 제거하며, `BattleStatusEffectService`의 특수 연출만 임시 방향을 적용하고 원래 방향으로 복원한다. 실행 상태 변경은 기존 `BattleActionRunner`가 담당한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-23-monster-execution-time-facing-design.md`

## Global Constraints

- 문서는 `AI_Docs`, 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode와 Unity 에디터 직접 제어는 수행하지 않는다.
- 특수 연출 방향은 유지한다.
- 커밋, Push, PR, 브랜치/worktree 작업은 수행하지 않는다.

## Review Focus

- 첫 행동의 반대 방향 플립이 계획 명령에 남아야 한다.
- 일반 예약 완료가 `MonsterRuntimeData.Direction`을 변경하면 안 된다.
- 포털 연출 종료 후 첫 행동 방향이 아니라 연출 전 방향으로 복원되어야 한다.
- 아라벨라 거미줄 완료가 일반 첫 행동 방향을 선행 적용하면 안 된다.
- 호스트와 클라이언트 모두 같은 `FlipBeforeAction` 명령을 실행해야 한다.

### Task 1: 방향 정책 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

- [ ] 첫 행동도 반대 방향이면 플립이 필요하다는 테스트를 작성하고 RED를 확인한다.
- [ ] 특수 연출 방향 복원이 저장된 원래 방향을 반환한다는 테스트를 작성하고 RED를 확인한다.

### Task 2: 일반 선행 플립 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleMonsterTurnPlanner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs`

- [ ] 예약 완료의 `ApplyAllFirstReservedActionFacings` 호출을 제거한다.
- [ ] 무료 턴 시작 연출 전후와 거미줄 완료 후 첫 예약 방향 적용 호출을 제거한다.
- [ ] 명령 계획의 `AssignMonsterMergedFlips`와 실행 러너의 플립 적용은 유지한다.

### Task 3: 특수 연출 방향 보존

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleMonsterTurnPlanner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs`

- [ ] 녹턴 포털 연출 직전 방향을 저장한다.
- [ ] 포털 방향 적용과 포털 애니메이션은 유지한다.
- [ ] 정상·대체 경로 모두 연출 후 저장 방향으로 복원한다.

### Task 4: 검증

- [ ] 관련 EditMode 테스트 소스 포함 에디터 어셈블리를 컴파일한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 네트워크 스냅샷 필드와 실제 실행 플립 경로가 유지됐는지 정적 확인한다.

