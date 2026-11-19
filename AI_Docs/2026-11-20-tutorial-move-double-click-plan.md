# Tutorial Move Double Click Guard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 튜토리얼 이동 안내 단계에서 이동 버튼을 빠르게 두 번 눌러도 선택이 취소되어 진행이 막히지 않게 한다.

**Architecture:** 이동 선택 토글 API가 재클릭 취소 허용 여부를 입력으로 받는다. 일반 전투는 기본 취소 동작을 유지하고, 튜토리얼 표현 중인 패널 호출만 취소를 금지한다.

**Tech Stack:** Unity, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-20-tutorial-move-double-click-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 명령, 예약 결과, 네트워크 동기화 구조는 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 튜토리얼에서 동일 이동 선택 재클릭이 현재 선택을 유지하는가.
- 일반 전투에서 동일 이동 선택 재클릭이 기존처럼 취소되는가.
- 다른 캐릭터 또는 다른 이동 스킬 선택은 취소 방어와 무관하게 정상 선택되는가.
- null 입력의 기존 실패 동작이 유지되는가.
- 튜토리얼 입력 방어가 확정 예약이나 네트워크 상태에 영향을 주지 않는가.

---

### Task 1: 이동 선택 토글의 튜토리얼 재클릭 방어

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: `TutorialBattleSequence.IsPresentationRunning`
- Produces: `BattleTimelineController.ToggleMoveSkillSelection(CharacterRuntimeData, SkillMasterData, bool allowDeselect = true)`

- [x] **Step 1: 실패 회귀 테스트 작성**

`TutorialMoveSelection_RepeatedClickKeepsSelectionWhenDeselectIsDisabled`와 `RegularMoveSelection_RepeatedClickStillCancelsSelection`을 추가한다. 전자는 `allowDeselect: false` 호출 후 선택 유지와 성공 반환을, 후자는 기본 호출 후 기존 취소와 실패 반환을 검증한다.

- [x] **Step 2: 테스트가 새 인터페이스 부재로 실패하는지 확인**

Run: 프로젝트 EditMode 테스트 어셈블리 컴파일
Expected: 새 `allowDeselect` 인자를 받는 오버로드가 없어 컴파일 실패

- [x] **Step 3: 최소 구현**

`ToggleMoveSkillSelection`에 `bool allowDeselect = true`를 추가하고 동일 선택에서 취소가 비허용이면 현재 선택을 유지한다. 패널의 이동 버튼 호출은 튜토리얼 표현 중 `allowDeselect: false`를 전달하고, 실제 선택 유지/생성 성공일 때만 이동 선택 알림을 보낸다.

- [x] **Step 4: 회귀 테스트와 프로젝트 컴파일 검증**

Run: 프로젝트 EditMode 테스트 어셈블리 컴파일 및 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`
Expected: exit code 0, 컴파일 오류 0

- [x] **Step 5: 정적 검증**

Run: 변경 파일 대상 `git diff --check` 및 `git diff`
Expected: 공백 오류 없음, 계획 범위 밖 변경 없음
