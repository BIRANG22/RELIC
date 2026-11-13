# Nocturn Facing And Kaya Retreat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 녹턴 포털 후속 공격 방향과 카야 비연검의 명중 여부와 무관한 자연스러운 동시 후퇴를 수정한다.

**Architecture:** 예약된 방향과 그리드 이동 결과를 전투 상태의 단일 기준으로 유지한다. 비연검은 분리된 이동 코루틴 대신 공격 실행 함수 내부에서 이동을 완료한 후 피해 연출을 시작한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-14-nocturn-facing-kaya-retreat-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode와 직접 에디터 제어는 사용하지 않는다.
- 전투 결과와 연출을 분리하고 안정적인 런타임 ID/그리드 상태를 기준으로 한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 수평 포털 이동 성공 및 실패 모두 동일한 예약 방향을 유지한다.
- 수직 포털 이동은 기존 좌우 방향을 변경하지 않는다.
- 공격 후퇴가 막힌 경우 현재 그리드 위치를 유지한다.
- 공격 연출과 후퇴 이동의 동시 실행은 유지한다.
- 일반 플레이어 이동과 다른 몬스터 이동에는 영향을 주지 않는다.

---

### Task 1: 녹턴 포털 방향 상태 동기화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Consumes: `BattleUnitFacing.GetBattleDirection()`, `MonsterRuntimeData.Direction`
- Produces: 포털 이동 직후 일치하는 화면/런타임 방향

- [ ] 포털 방향 상태 동기화가 없는 현재 코드에서 실패하는 회귀 테스트를 작성한다.
- [ ] 테스트가 기대한 이유로 실패하는지 확인한다.
- [ ] `ExecuteNocturnPortalMove`에서 수평 목적지 방향을 런타임 상태에 기록한다.
- [ ] 회귀 검사를 다시 실행한다.

### Task 2: 카야 병렬 후퇴 월드 위치 재동기화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/CombatAiPresentationFixPolicyTests.cs`

**Interfaces:**
- Consumes: `BattleCharacter.CurrentGridIndex`, `GridManager.GetWorldPositionByIndex(int)`
- Produces: 공격 애니메이션과 함께 완료되는 보간 후퇴 및 이동 후 피격 판정

- [ ] 분리된 이동 코루틴을 사용하는 현재 코드에서 실패하는 회귀 테스트를 작성한다.
- [ ] 테스트가 기대한 이유로 실패하는지 확인한다.
- [ ] 실패한 병렬 코루틴 코드를 제거하고 공격 함수 내부에서 이동 완료 후 피해를 처리한다.
- [ ] 회귀 검사, diff 검사, 런타임/에디터 컴파일을 수행한다.

