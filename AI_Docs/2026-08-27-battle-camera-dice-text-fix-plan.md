# Battle Camera Dice Text Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투방 이후 이벤트/휴식 방에서 카메라 줌 상태와 이벤트 주사위 UI가 남지 않도록 정리하고, 소스에 남은 깨진 문자열을 복구한다.

**Architecture:** 전투 결과 계산은 건드리지 않고, 룸 전환과 이벤트 UI 표시 계층에서만 상태 정리를 수행한다. 카메라는 기존 `BattleCameraController`의 즉시 복귀 API를 재사용하고, 주사위는 `EventDiceRollPresenter`에 명시적 숨김 API를 추가해 `EventRoomController` 생명주기에서 호출한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, MSBuild compile verification.

**Spec:** 사용자 요청: 전투방 카메라 줌이 이벤트 방에 남는 문제, 진행 버튼 후 주사위 UI가 남는 문제, 깨진 텍스트 복구.

## Global Constraints
- 문서는 `AI_Docs` 폴더 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/` 아래에만 작성한다.
- Unity 에디터는 열려 있다고 가정하고 batchmode 테스트는 실행하지 않는다.
- 전투 핵심 결과 계산은 수정하지 않고 UI/카메라/사운드 연출 계층만 수정한다.
- 커밋, Push, PR, 브랜치 생성/전환은 수행하지 않는다.

---

### Task 1: Camera Reset On Non-Battle Room Entry

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify/Test: `Assets/Tests/EditMode~/BattleCameraControllerMonsterInfoFocusTests.cs`

**Interfaces:**
- Consumes: `BattleCameraController.ForceReturnMapImmediate()`
- Produces: non-battle room entry calls the existing map camera reset path before room activation.

- [x] **Step 1: Write failing test**

Add a test proving `ForceReturnMapImmediate()` restores camera position, rotation, size, combat zoom flag, and monster info focus flag after a zoom/focus state.

- [x] **Step 2: Verify RED**

Run source boundary checks before production implementation. Expected first failure: `EventDiceRollPresenter.HideImmediate()` and `BattleSceneController` non-battle camera reset hook are missing.

- [x] **Step 3: Implement**

Add a small helper in `BattleSceneController` that resets camera to map position for non-battle rooms, and call it from `OpenRoom()` before activating an event/rest room.

- [x] **Step 4: Verify GREEN**

Run editor assembly build and confirm it compiles.

### Task 2: Dice Presenter Cleanup

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventDiceRollPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- Modify/Test: `Assets/Tests/EditMode~/EventDiceRollPresentationTests.cs`

**Interfaces:**
- Produces: `EventDiceRollPresenter.HideImmediate()`
- Consumes: `EventRoomController.HideDiceRollPresenterImmediate()` calls presenter cleanup on room entry, room exit, next-button completion, reward completion, and next event load.

- [x] **Step 1: Write failing test**

Add a test that plays the dice presenter, calls `HideImmediate()`, and asserts that the presenter is inactive and the completion callback from the stopped roll is not invoked later.

- [x] **Step 2: Verify RED**

Run source/API boundary checks. Expected first failure: `HideImmediate()` does not exist in production.

- [x] **Step 3: Implement**

Add `HideImmediate()` to stop the active roll coroutine, stop animation, and deactivate the presenter. Call it from event room reset and transition paths.

- [x] **Step 4: Verify GREEN**

Run editor assembly build and confirm it compiles.

### Task 3: Corrupted Text Cleanup

**Files:**
- Modify corrupted strings under `Assets/Project/Scripts/**/*.cs`

**Interfaces:**
- Produces: no replacement-character literals remain in scripts except intentional detection expressed as `"\uFFFD"`.

- [x] **Step 1: Replace user-facing default strings**

Use normal scene YAML values as references where available:
- `아직 준비되지 않았습니다.`
- `정말 포기하시겠습니까?`
- `예`
- `아니오`
- `포기할 탐사 정보가 없음`
- `진행 중인 탐사를 포기했습니다.`
- `아직 입장할 수 없는 구역입니다.`
- `스테이지를 선택해야 합니다.`
- `캐릭터를 편성해야 합니다.`
- `캐릭터 3명을 모두 편성해야 합니다. 현재 {0}/{1}`
- `데이터 매니저가 없습니다.`
- `게임 매니저가 없습니다.`
- `다시 사용하려면 회복할 시간이 필요합니다.`
- `전투 시작`
- `행동 예약`

- [x] **Step 2: Replace corrupted tooltips/comments/logs**

Where exact Korean wording cannot be recovered safely, use clear English comments or warnings to avoid further encoding damage.

- [x] **Step 3: Verify**

Run a replacement-character search against `Assets/Project/Scripts` and confirm only intentional `\uFFFD` representation remains, then run editor assembly build.
