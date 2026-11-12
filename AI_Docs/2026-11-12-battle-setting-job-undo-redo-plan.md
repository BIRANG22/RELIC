# Battle Setting Job Icon and Undo/Redo Buttons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배틀 설정 패널에 파티 캐릭터의 직업 아이콘을 표시하고 취소/복원 버튼을 예약 행동 Undo/Redo에 연결한다.

**Architecture:** `BattleSettingPanelUI`는 기존 `CharacterIconDatabase`를 통해 직업 아이콘을 조회하고 `Job/Image`에 표시한다. `BattleCharacterPanelUI`는 새 버튼을 기존 `BattleTimelineController`의 Undo/Redo 공개 API에 위임하여 마우스 입력과 동일한 상태 변경 경로를 사용한다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-12-battle-setting-job-undo-redo-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 전투 핵심 상태는 UI에서 직접 변경하지 않고 기존 타임라인 API를 사용한다.
- Unity batchmode 및 에디터 직접 제어는 수행하지 않는다.
- 커밋, Push, PR은 사용자 추가 승인 없이는 수행하지 않는다.

## Review Focus

- 캐릭터 ID가 비어 있거나 DB에 직업 아이콘이 없으면 기존 이미지가 남지 않고 숨겨져야 한다.
- 패널을 다시 열거나 파티 구성이 바뀌면 직업 아이콘도 현재 캐릭터에 맞춰 갱신되어야 한다.
- 취소 버튼은 현재 입력 잠금과 히스토리 조건을 우회하지 않아야 한다.
- 복원 버튼은 redo 기록이 없을 때 예약 상태를 변경하지 않아야 한다.
- 새 버튼 연결 뒤 제거된 Reset 동작이 의도치 않게 실행되지 않아야 한다.

---

### Task 1: 직업 아이콘 표시

**Files:**
- Modify: `Assets/Project/Scripts/UI/BattleSettingPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleSettingPanelUITests.cs`

**Interfaces:**
- Consumes: `CharacterIconDatabase.TryGetMark(string, out Sprite)`
- Produces: 각 `CharacterView.Job` 이미지에 현재 캐릭터의 직업 아이콘 표시

- [ ] **Step 1: 실패 테스트 작성** — 캐릭터 DB의 `Mark`가 직업 이미지에 적용되고 누락 시 이미지가 비활성화되는 동작을 검증한다.
- [ ] **Step 2: 테스트 실행 및 RED 확인** — 기존 코드에는 Job 이미지 연결이 없어 기대한 실패가 발생해야 한다.
- [ ] **Step 3: 최소 구현** — `CharacterView`에 Job 이미지를 추가하고 `ResolveViews`에서 `Job/Image`를 찾은 뒤 `Apply`에서 `TryGetMark` 결과를 적용한다.
- [ ] **Step 4: 테스트 실행 및 GREEN 확인** — 신규 테스트와 기존 `BattleSettingPanelUITests`가 통과해야 한다.

### Task 2: 취소/복원 버튼 연결

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/BattleCharacterPanelUtilityButtonTests.cs`

**Interfaces:**
- Consumes: `BattleTimelineController.UndoLastPlayerReservation()`, `BattleTimelineController.RedoLastPlayerReservation()`
- Produces: `CancelButton` 클릭 시 Undo, `RestoreButton` 클릭 시 Redo

- [ ] **Step 1: 실패 테스트 작성** — 버튼 클릭이 각각 타임라인 Undo/Redo와 동일한 예약 결과를 만드는지 검증한다.
- [ ] **Step 2: 테스트 실행 및 RED 확인** — 기존 코드는 새 버튼을 찾거나 연결하지 않으므로 실패해야 한다.
- [ ] **Step 3: 최소 구현** — reset 필드를 cancel/restore 필드로 교체하고 참조 탐색, 리스너 등록/해제, 클릭 처리기를 기존 Undo/Redo API에 연결한다.
- [ ] **Step 4: 씬 참조 갱신** — `Battle.unity`의 `BattleCharacterPanelUI`에 `CancelButton`과 `RestoreButton` Button 컴포넌트를 직렬화한다.
- [ ] **Step 5: 테스트 실행 및 GREEN 확인** — 신규 버튼 테스트와 타임라인 수동 제어 테스트가 통과해야 한다.

### Task 3: 전체 검증

**Files:**
- Verify: 위 변경 파일 전체

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: 컴파일 및 회귀 검증 근거

- [ ] **Step 1: 관련 EditMode 테스트 실행** — 비-batchmode 방식으로 프로젝트에서 허용된 테스트 실행 경로를 사용한다.
- [ ] **Step 2: 런타임/에디터 어셈블리 컴파일** — 두 csproj 빌드가 오류 없이 끝나야 한다.
- [ ] **Step 3: 씬 YAML 검증** — 버튼 fileID와 컴포넌트 참조, Job 이미지 경로가 일치하는지 확인한다.
- [ ] **Step 4: diff 검토** — 요청 범위 밖 변경과 사용자 기존 변경을 건드리지 않았는지 확인한다.

