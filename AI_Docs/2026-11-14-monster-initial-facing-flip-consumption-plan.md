# Monster Initial Facing Flip Consumption Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 턴 시작 전에 적용된 몬스터의 첫 행동 방향 전환을 타임라인 명령에서 소비한다.

**Architecture:** 기존 예약 방향 계산은 유지한다. 실제 첫 방향을 적용하는 단일 경계에서 해당 명령의 표시 및 실행 플립 플래그만 해제한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-14-monster-initial-facing-flip-consumption-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode와 에디터 직접 제어는 사용하지 않는다.
- 전투 상태와 타임라인 표시는 같은 명령 상태를 사용한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 첫 공격 방향을 선반영한 경우 실행 플립과 표시 플립을 모두 제거한다.
- 첫 수평 이동 방향을 선반영한 경우 이동 플립 표시를 제거한다.
- 방향 없는 첫 행동은 후속 방향 플립을 소비하지 않는다.
- 수직 이동은 기존 방향과 플립 정책을 유지한다.
- 두 번째 이후 행동의 방향 전환은 계속 표시하고 실행한다.

---

### Task 1: 첫 방향 플립 소비

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleMonsterTurnPlanner.cs`
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Consumes: `MonsterReservedCommand.SetFlipBeforeAction(bool)`, `SetShowMovementFacingTransition(bool)`
- Produces: 첫 방향 선반영이 끝난 명령의 소비된 플립 상태

- [ ] 현재 코드에서 실패하는 방향 적용 후 플립 소비 순서 테스트를 작성한다.
- [ ] 테스트가 기대한 이유로 실패하는지 확인한다.
- [ ] 실제 방향 적용 성공 분기에서 두 플립 플래그를 해제한다.
- [ ] 회귀 검사, diff 검사, 런타임 및 에디터 컴파일을 수행한다.

