# Resume Phase Checkpoint Implementation Plan

**Goal:** 모든 진행 phase가 현재 Runtime과 확정된 Resume payload를 저장하고 Continue에서 같은 상태를 재실행 없이 복원한다.

**Architecture:** SaveSystem의 일반 진행 저장과 BattleEntry rollback snapshot을 분리한다. 전투·이벤트는 결과를 Runtime에 적용한 뒤 ResumeData를 저장하고, BattleSceneController가 phase별 복원 UI를 라우팅한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests.

**Spec:** `AI_Docs/2026-09-06-resume-phase-checkpoint-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 결과는 UI/VFX와 분리하고 확정된 Runtime/ID 기반 데이터만 저장한다.
- 커밋, Push, PR, 브랜치·worktree 작업은 수행하지 않는다.

### Task 1: Resume payload와 저장 경계

**Files:** `ResumeData.cs`, `SaveSystem.cs`, `SaveSystemSnapshotTests.cs`

- [ ] Battle/Event 보상 수령 상태와 EventChoice 적용 완료 상태를 표현하는 실패 테스트를 작성한다.
- [ ] 일반 checkpoint가 현재 Runtime + ResumeData만 저장하고 BattleEntry rollback cache를 덮어쓰지 않는 최소 API를 구현한다.
- [ ] 직렬화 및 snapshot 테스트를 실행한다.

### Task 2: BattleEntry 및 BattleReward

**Files:** `BattleSceneController.cs`, `BattleResultChecker.cs`, `BattleRewardPanelUI.cs`, 관련 EditMode 테스트

- [ ] 확정된 보상을 저장 데이터로 변환하는 실패 테스트를 작성한다.
- [ ] 전투 초기화 완료 후 BattleEntry를 저장하고 승리 시 보상 확정·저장 후 패널을 열도록 연결한다.
- [ ] 재개 시 저장 보상 패널을 열고 지급 직후 수령 상태를 저장하도록 구현한다.

### Task 3: EventEntry, Choice, Dice, Reward

**Files:** `EventRoomController.cs`, `EventDiceRollPresenter.cs`, `EventChoiceExecutionService.cs`, 관련 EditMode 테스트

- [ ] 저장 DiceFaces와 적용 완료 Choice가 재실행되지 않는 실패 테스트를 작성한다.
- [ ] 이벤트 선택/선택 결과/주사위 결과/보상 확정 순서로 ResumeData 저장을 연결한다.
- [ ] Continue에서 EventEntry·Choice·Dice·pending reward를 UI에 복원한다.

### Task 4: Continue 라우팅과 Rest 호환

**Files:** `TitleContinueButton.cs`, `BattleSceneController.cs`, `RestRoomController.cs`, 관련 EditMode 테스트

- [ ] phase가 지정된 저장이 Continue 가능하고 올바른 복원 경로를 선택하는 실패 테스트를 작성한다.
- [ ] BattleSceneController phase router와 기존 Rest 상태 복원을 연결한다.
- [ ] autosave 억제가 복원 완료 시에만 풀리는지 검증한다.

### Task 5: 검증

- [ ] 영향 테스트를 실행하고 컴파일 오류를 확인한다.
- [ ] `git diff --check`와 변경 파일 diff를 검토한다.
- [ ] 구현하지 못한 scene-dependent 수동 검증 항목을 명시한다.
