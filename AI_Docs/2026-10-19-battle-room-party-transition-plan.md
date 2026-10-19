# 전투방 파티 진입·퇴장 이동 연출 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 노드 진입·퇴장 시 기존 화면 전환과 병렬로 플레이어 캐릭터가 동시에 좌우 이동하며 페이드되도록 한다.

**Architecture:** `BattleRoomPartyTransitionPresenter`가 전투 캐릭터 Transform, SpriteRenderer 알파, `BattleUnitAnimator`만 제어한다. `BattleRoomLoader`는 스폰 직후 진입 연출을 시작하고, `BattleSceneController`는 전투방에서 맵으로 돌아갈 때 퇴장 연출과 기존 화면 가림을 함께 기다린다. 전투 런타임 데이터와 그리드 점유는 변경하지 않는다.

**Tech Stack:** Unity 6, C#, Coroutine/Task, Text-free world SpriteRenderer, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-19-battle-room-party-transition-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/` 아래에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 현재 브랜치와 작업 공간을 사용하며 사용자 승인 없이 커밋, Push, PR, 브랜치 또는 worktree 작업을 하지 않는다.
- 이동은 표현 계층이며 `CharacterId`, `CurrentGridIndex`, 그리드 점유, 전투 상태와 노드 완료 상태를 변경하지 않는다.
- 세 캐릭터는 동일한 정규화 시간으로 동시에 이동한다.
- 기존 화면 전환의 시작·종료 위치와 노드 결과를 변경하지 않는다.

## Review Focus

- 전투방에 캐릭터가 0~2명뿐인 경우 존재하는 캐릭터만 처리하고 완료 대기가 끝나야 한다.
- Animator 또는 SpriteRenderer가 없는 캐릭터도 이동과 전환 완료를 방해하지 않아야 한다.
- 연출 도중 캐릭터나 전투방이 비활성화·파괴되면 Task/Coroutine이 영구 대기하지 않아야 한다.
- 저장 복원과 네트워크 즉시 적용 경로는 연출 중간 좌표를 남기지 않아야 한다.
- 퇴장 시 전투 중 변경된 현재 월드 위치를 시작점으로 사용하고 런타임 그리드 인덱스를 변경하지 않아야 한다.

---

### Task 1: 이동 좌표 및 동시 보간 규칙

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleRoomPartyTransitionMath.cs`
- Create: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Produces: `BattleRoomPartyTransitionMath.GetEntryStart(Vector3 finalPosition, Vector3 entryOffset)`, `GetExitTarget(Vector3 currentPosition, Vector3 exitOffset)`, `Evaluate(Vector3 from, Vector3 to, float normalizedTime, AnimationCurve curve)`

- [ ] **Step 1: Write the failing coordinate tests**
  - `GetEntryStart_AddsEntryOffsetToFinalPosition`
  - `GetExitTarget_AddsExitOffsetToCurrentPosition`
  - `Evaluate_UsesOneNormalizedTimeForEveryCharacter`
  - 각 테스트는 정확한 Vector3 결과와 0/1 경계값을 검증한다.

- [ ] **Step 2: Verify RED**
  - `Assembly-CSharp-Editor.csproj`에 테스트 파일을 포함한 임시 MSBuild target으로 컴파일한다.
  - 예상 결과: `BattleRoomPartyTransitionMath`가 없어 컴파일 실패.

- [ ] **Step 3: Implement the pure presentation math**
  - 위 세 static 메서드를 구현한다.
  - `Evaluate`는 `Mathf.Clamp01`과 전달된 커브를 사용하며 커브가 null이면 선형 보간한다.

- [ ] **Step 4: Verify GREEN**
  - 동일한 MSBuild 명령으로 런타임·테스트 어셈블리 컴파일 성공을 확인한다.

### Task 2: 파티 이동·알파·애니메이션 Presenter

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleRoomPartyTransitionPresenter.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleRoomPartyTransitionPresenter.cs.meta`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Consumes: Task 1의 `BattleRoomPartyTransitionMath`
- Produces: `PrepareEntry()`, `Task PlayPreparedEntryAsync()`, `Task PlayExitAsync()`, `CancelAndRestore(bool restoreEntryFinalPositions)`, `bool IsPlaying`

- [ ] **Step 1: Write failing presenter tests**
  - `PrepareEntry_MovesAllPlayersLeftWithoutChangingGridIndex`
  - `CompleteEntry_RestoresEveryCapturedFinalPositionAndAlpha`
  - `PrepareExit_UsesEachCharactersCurrentPosition`
  - `MissingAnimatorOrRenderer_DoesNotBlockCompletion`
  - `DestroyedCharacter_DoesNotBlockCompletion`
  - 테스트용 GameObject에 1~3개의 `BattleCharacter`를 만들고 Transform과 런타임 그리드 값을 전후 비교한다.

- [ ] **Step 2: Verify RED**
  - 테스트 포함 컴파일에서 Presenter 타입/API 부재로 실패하는지 확인한다.

- [ ] **Step 3: Implement serialized presentation settings**
  - `entryOffset`, `exitOffset`, `entryDuration`, `exitDuration`, `entryCurve`, `exitCurve`, `fadeCharacters`를 직렬화한다.
  - 기본 오프셋은 entry X 음수, exit X 양수이며 duration은 현재 `BattleDiagonalSceneTransition` 구간에 맞춰 인스펙터에서 조정 가능하게 한다.

- [ ] **Step 4: Implement actor capture and restoration**
  - Presenter가 속한 전투방 자식의 플레이어 `BattleCharacter`만 수집한다.
  - 각 actor에 Transform 최종/현재 위치, SpriteRenderer별 원래 Color, `BattleUnitAnimator`를 캡처한다.
  - 사망 또는 비활성 캐릭터를 강제로 활성화하지 않는다.

- [ ] **Step 5: Implement entry and exit playback**
  - 하나의 unscaled normalized time으로 모든 actor를 같은 프레임에 보간한다.
  - 이동 시작 시 `PlayMove()`, 정상 완료·취소 시 `PlayIdle()`을 호출한다.
  - 진입 완료 위치는 캡처한 그리드 월드 위치로 정확히 스냅한다.
  - 퇴장 완료 후 원래 색상은 보관하되 방 정리 전에 중간 상태가 남지 않도록 취소·비활성화 경로를 정리한다.

- [ ] **Step 6: Verify GREEN**
  - Presenter 테스트와 전체 런타임·에디터 어셈블리 컴파일 성공을 확인한다.

### Task 3: 전투방 스폰 이후 진입 연출 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Consumes: `BattleRoomPartyTransitionPresenter.PrepareEntry()`, `PlayPreparedEntryAsync()`
- Produces: 스폰 완료 후 진입 연출 시작과 입력 준비 게이트

- [ ] **Step 1: Write failing loader contract tests**
  - `BattleRoomLoader_StartsEntryAfterPlayersAreSpawned`
  - `BattleRoomLoader_KeepsInputDisabledUntilEntryCompletes`
  - `BattleRoomLoader_MissingPresenterPreservesExistingLoadFlow`
  - 소스 구조 테스트는 `SpawnPlayersAndHUD()` 뒤에 Presenter 준비가 호출되고 `SetBattleInputReady(true)`보다 완료 대기가 앞서는지 검증한다.

- [ ] **Step 2: Verify RED**
  - 기존 Loader에 Presenter 연동이 없어 테스트 실패를 확인한다.

- [ ] **Step 3: Add explicit presenter reference**
  - `BattleRoomLoader`에 `[SerializeField] private BattleRoomPartyTransitionPresenter partyTransitionPresenter;`를 추가한다.
  - 장기 경로는 씬 직렬화 참조를 사용하고, 누락 시에만 같은 전투방 자식에서 호환 탐색한다.

- [ ] **Step 4: Connect entry lifecycle**
  - `SpawnPlayersAndHUD()` 직후 최종 위치를 캡처하고 진입을 준비한다.
  - 기존 입력 비활성화는 유지하며 초기 몬스터 계획/입력 준비 흐름에서 진입 완료를 기다린다.
  - 복원·즉시 로드 경로는 이동 없이 최종 상태로 정리한다.

- [ ] **Step 5: Verify GREEN**
  - Loader 계약 테스트와 전체 컴파일을 확인한다.

### Task 4: 기존 화면 전환과 퇴장 연출 병렬 대기

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Consumes: `BattleRoomPartyTransitionPresenter.PlayExitAsync()`, 기존 `BattleDiagonalSceneTransition.PlayRoomToMapAsync(Action)`
- Produces: 전투방 퇴장 이동과 화면 가림이 모두 끝난 후 기존 room cleanup을 실행하는 흐름

- [ ] **Step 1: Write failing transition orchestration tests**
  - `RoomToMap_StartsPartyExitAndScreenCoverTogether`
  - `RoomToMap_CleansRoomOnlyAfterBothComplete`
  - `NonBattleRoom_DoesNotRequestPartyExit`
  - `MissingPresenter_UsesExistingTransitionFlow`
  - `CancelledOrDestroyedPresenter_DoesNotLeaveTransitionWaiting`

- [ ] **Step 2: Verify RED**
  - 기존 Controller가 캐릭터 퇴장 Task를 요청하지 않아 테스트 실패를 확인한다.

- [ ] **Step 3: Add battle-room-only presenter resolution**
  - 현재 활성 방이 `battleRoom`인 경우에만 해당 방 자식의 Presenter를 사용한다.
  - 이벤트·휴식·시작 노드 경로는 기존 코드 그대로 유지한다.

- [ ] **Step 4: Orchestrate concurrent exit**
  - 퇴장 Task와 기존 화면 가림을 같은 시점에 시작한다.
  - 둘 다 완료된 뒤 기존 `onCovered`, `PrepareRoomForMapSelection`, `OpenMapPanelImmediate` 순서를 유지한다.
  - already-covered, 외부 비활성화, 네트워크 즉시 적용 경로에서는 Presenter를 즉시 정리하고 대기하지 않는다.

- [ ] **Step 5: Verify GREEN**
  - 오케스트레이션 테스트와 전체 컴파일을 확인한다.

### Task 5: Battle 씬 직렬화 및 최종 검증

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `AI_Docs/2026-10-19-battle-room-party-transition-design.md` (구현 중 확정된 기본값만 반영)

**Interfaces:**
- Consumes: Task 2~4의 Presenter와 Loader 참조
- Produces: 실제 Battle 씬에서 조정 가능한 연출 설정

- [ ] **Step 1: Add Presenter to the BattleRoom hierarchy**
  - 전투방의 명시적 연출 오브젝트에 Presenter를 배치한다.
  - Loader의 `partyTransitionPresenter`에 해당 컴포넌트를 직렬화 연결한다.
  - entry는 왼쪽, exit는 오른쪽 오프셋으로 설정하고 기존 화면 transition duration에 맞춘다.

- [ ] **Step 2: Validate serialized references**
  - 씬 YAML에서 Presenter 컴포넌트, Loader 참조, 오프셋 방향, duration 값이 모두 저장됐는지 확인한다.

- [ ] **Step 3: Run fresh compile verification**
  - `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
  - 성공 조건: 신규 오류 0개. 기존 경고는 별도로 기록한다.

- [ ] **Step 4: Run non-batch Unity verification where available**
  - Unity Test Runner를 제어할 수 있으면 관련 EditMode 테스트를 실행한다.
  - 제어할 수 없으면 미검증 항목으로 명시하며 batchmode는 실행하지 않는다.

- [ ] **Step 5: Inspect final diff and multiplayer boundary**
  - `git diff --check`를 실행한다.
  - 전투 런타임 데이터, Command, State Change, Result/Event 코드가 변경되지 않았는지 확인한다.
  - 커밋, Push, PR은 수행하지 않는다.
