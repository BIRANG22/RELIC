# Battle Room Map Selection Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배틀씬을 StartRoom에서 시작하고, 룸을 유지한 채 하단 가로 지도와 별도 다음 노드 버튼으로 다음 경로를 선택하게 한다.

**Architecture:** 기존 `MapRuntimeData`와 노드 연결 구조를 보존하고 선택 검증을 순수 런타임 유틸리티에 둔다. 지도 렌더링, 다음 노드 버튼, 룸 종료 프레젠테이션을 분리하고 `BattleSceneController`가 전환 경계를 조율한다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs` 안에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 상태 변경과 UI/VFX/사운드 표시를 분리한다.
- 상태 선택은 `NodeIndex`, 캐릭터 식별은 `CharacterId`를 사용한다.
- 좌표 랜덤은 제거하되 연결 생성 랜덤은 `BattleRandom`을 유지한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

---

### Task 1: 맵 진행 유틸리티와 가로 고정 좌표

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/MapRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`
- Test: `Assets/Tests/EditMode~/MapRuntimeProgressUtilityTests.cs`
- Test: `Assets/Tests/EditMode~/BattleHorizontalMapLayoutTests.cs`

**Interfaces:**
- Produces: `FindStartNode(MapRuntimeData)`, `CollectSelectableNextNodes(MapRuntimeData, int)`, 고정 `GeneratedMapNodeData.Position`.

- [ ] 선택 가능한 다음 노드 순서·상한·무효 상태 테스트를 먼저 작성한다.
- [ ] 맵 생성 결과가 레이어별로 X가 증가하고 동일 seed에서도 좌표 jitter가 없음을 검증하는 테스트를 작성한다.
- [ ] 에디터에서 테스트가 실패함을 확인한다.
- [ ] 진행 유틸리티와 고정 가로 좌표 계산을 최소 구현한다.
- [ ] 대상 테스트를 에디터에서 다시 실행한다.

### Task 2: 읽기 전용 가로 지도와 다음 노드 선택 패널

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapViewSpawner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapNodeView.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleNextNodeSelectionPanel.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleNextNodeChoiceButton.cs`
- Test: `Assets/Tests/EditMode~/BattleMapSelectionUiTests.cs`

**Interfaces:**
- Consumes: `CollectSelectableNextNodes(MapRuntimeData, int)`.
- Produces: `BattleNextNodeSelectionPanel.Open(MapRuntimeData, Action<int>)`, 읽기 전용 `BattleMapPanel.Open`.

- [ ] 지도 노드가 이동 콜백을 받지 않고 선택 패널이 최대 3개의 `NodeIndex`를 노출하는 실패 테스트를 작성한다.
- [ ] 에디터에서 실패를 확인한다.
- [ ] 지도 스크롤의 가로 포커스와 별도 선택 버튼 바인딩을 구현한다.
- [ ] 대상 테스트를 에디터에서 다시 실행한다.

### Task 3: 룸 유지형 지도 선택 프레젠테이션

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoomMapSelectionPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: 각 룸 완료 컨트롤러의 지도 복귀 호출부
- Test: `Assets/Tests/EditMode~/BattleRoomMapSelectionPresenterTests.cs`
- Test: `Assets/Tests/EditMode~/BattleSceneStartFlowTests.cs`

**Interfaces:**
- Produces: `Show(GameObject activeRoom, MapRuntimeData runtime)`, `Hide()`, `EnterMapSelectionFromRoom()`.
- Consumes: `BattleDiagonalSceneTransition.PlayRoomToMapAsync`, `NodeIndex` 선택 요청.

- [ ] 활성 룸 유지, 지정 UI 비활성화, 캐릭터 슬롯 배치, 새 런 Start 자동 진입 테스트를 먼저 작성한다.
- [ ] 에디터에서 실패를 확인한다.
- [ ] 전환막 콜백에서 프레젠터를 적용하고 기존 `CloseAllRooms()` 호출을 제거한다.
- [ ] 선택 요청을 런타임 검증 후 기존 Map→Room 전환에 연결한다.
- [ ] 대상 테스트를 에디터에서 다시 실행한다.

### Task 4: Battle 및 DebugBattle 씬 UI 연결

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Test: `Assets/Tests/EditMode~/BattleMapSelectionSceneConfigurationTests.cs`

**Interfaces:**
- Connects: 하단 가로 `ScrollRect`, 노드/연결선 루트, 최대 3개 선택 버튼, 룸별 숨김 UI, 캐릭터 배치 기준점.

- [ ] 두 씬에 필요한 오브젝트와 직렬화 참조가 존재하는 실패 테스트를 작성한다.
- [ ] 에디터에서 실패를 확인한다.
- [ ] 기존 MapPanel을 하단 지도 레이아웃으로 재배치하고 다음 노드 선택 영역 및 프레젠터 참조를 연결한다.
- [ ] Battle과 DebugBattle 설정을 동일하게 맞춘다.
- [ ] 씬 설정 테스트를 에디터에서 다시 실행한다.

### Task 5: 통합 검증

**Files:**
- Verify all modified files.

**Interfaces:**
- Verifies: 컴파일, 정적 규칙, EditMode 테스트 결과.

- [ ] `git diff --check`로 공백 및 패치 오류를 확인한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] Unity 에디터에서 관련 EditMode 테스트를 실행하고 결과를 확인한다.
- [ ] 최초 진입, 각 룸 종료, 지도 스크롤, 세 선택 버튼, 다음 룸 진입을 에디터에서 수동 확인한다.
- [ ] 변경 파일과 검증 결과 및 멀티플레이 영향 여부만 완료 보고한다.
