# Character Selection Camera Return And Skill Retreat Facing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 선택 캐릭터를 유지한 채 우클릭으로 카메라를 복귀시키고, 공격 스킬의 후퇴 이동 중 시전 방향을 유지한다.

**Architecture:** 기존 `BattleTimelineController`가 입력 우선순위와 카메라 요청을 소유하고, `BattleCameraController`의 선택 유지 전용 공개 API만 호출한다. `BattleActionRunner`는 일반 이동과 스킬 효과 이동을 구분하여 스킬 효과 이동에서는 Facing 상태를 변경하지 않는다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-03-character-selection-camera-return-and-skill-retreat-facing-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 기존 미커밋 변경을 보존한다.
- 커밋, Push, PR, 브랜치/worktree 작업을 수행하지 않는다.

## Review Focus

- 활성 스킬 선택이 있으면 카메라 복귀보다 선택 취소가 우선되어야 한다.
- 몬스터 정보창이 열려 있으면 기존 닫기 동작만 실행되어야 한다.
- 캐릭터가 선택되지 않은 우클릭은 기존 마지막 예약 취소를 유지해야 한다.
- 일반 이동은 계속 실제 이동 방향으로 Facing이 변경되어야 한다.
- 스킬 이동이 막히거나 시전자가 사망해도 방향 상태가 새로 변경되지 않아야 한다.

---

### Task 1: 선택 상태 우클릭 카메라 복귀

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Test: `Assets/Tests/EditMode~/BattleCharacterSelectionCameraReturnTests.cs`

**Interfaces:**
- Consumes: `BattleCameraController.StartReturnDefaultKeepingCharacterSelection()`
- Produces: 선택 캐릭터가 있는 유휴 우클릭의 카메라 복귀 및 입력 소비 규칙

- [ ] 소스 경계 테스트를 작성해 활성 선택 취소 다음, 예약 취소 이전에 선택 캐릭터 카메라 복귀 분기가 존재함을 고정한다.
- [ ] 테스트가 기존 코드에서 실패하는지 확인한다.
- [ ] `HandleSelectionCancelRightClick()`에 선택 상태 카메라 복귀 분기를 최소 구현한다.
- [ ] 선택 해제 필드와 이벤트를 변경하지 않고 해당 우클릭을 즉시 소비하는지 테스트한다.

### Task 2: 공격 스킬 이동 Facing 유지

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/BattleSkillDirectionalMoveFacingTests.cs`

**Interfaces:**
- Consumes: `PlayerReservedCommand.Direction`, `ExecutePlayerDirectionalMoveEffect(...)`
- Produces: 스킬 효과 이동 중 시전 방향을 보존하는 실행 규칙

- [ ] 소스 경계 테스트를 작성해 `ExecutePlayerDirectionalMoveEffect()`가 `FaceByMoveOffset`을 호출하거나 이동 후 Facing을 복구하는 임시 플립 경로를 포함하지 않음을 고정한다.
- [ ] 일반 이동의 `ApplyPlayerMoveFacing()` 경로는 유지된다는 회귀 테스트를 추가한다.
- [ ] 테스트가 기존 코드에서 실패하는지 확인한다.
- [ ] 스킬 효과 이동의 임시 Facing 변경과 복구 코드를 제거하고 위치·충돌·출혈·HUD 흐름은 보존한다.
- [ ] 관련 테스트가 통과하는지 확인한다.

### Task 3: 최종 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`
- Verify: Task 1~2 변경 파일 및 테스트

**Interfaces:**
- Consumes: Task 1~2 구현
- Produces: 컴파일과 정적 경계 검증 결과

- [ ] 새 EditMode 테스트를 가능한 비-batchmode 방식으로 실행하고 결과를 기록한다.
- [ ] 런타임 및 에디터 C# 프로젝트를 컴파일한다.
- [ ] 변경 diff에서 선택 해제, 일반 이동 Facing, 전투 결과 계산에 불필요한 변경이 없는지 확인한다.
- [ ] 에디터 화면에서만 확인 가능한 카메라 보간 및 이동 애니메이션 외형을 미검증 항목으로 분리한다.
