# Stage Background Controller Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 맵 행 범위에 맞는 Stage 01 배경 프리팹을 Start, Battle, Rest 방에 자동 표시하고 기존 `BattleSceneController`의 배경 관리 책임을 제거한다.

**Architecture:** `StageBackgroundController`가 직렬화된 행 범위와 프리팹을 소유하고 배경 인스턴스의 생명주기를 관리한다. `BattleSceneController`는 선택된 노드의 0-based `LayerIndex`만 전달한다.

**Tech Stack:** Unity 6, C#, Unity Test Framework, UnityEditor PrefabUtility

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 1~3행은 `St1_00`, 4~7행은 `St1_01`, 8~10행은 `St1_02`를 사용한다.
- `Boss` 배경은 수정하거나 연결하지 않는다.
- StartRoom, BattleRoom, RestRoom은 같은 세 배경 프리팹과 행 범위를 사용한다.
- 배경 코드는 전투 상태를 변경하지 않고 `LayerIndex`만 소비한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋과 PR은 별도 사용자 승인 전에는 수행하지 않는다.

---

### Task 1: 행 범위 선택 및 배경 인스턴스 관리

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/Background/StageBackgroundController.cs`
- Test: `Assets/Tests/EditMode~/StageBackgroundControllerTests.cs`

**Interfaces:**
- Consumes: `GeneratedMapNodeData.LayerIndex`의 0-based 정수
- Produces: `public void ShowForLayer(int layerIndex)`와 직렬화된 `BackgroundRange` 목록

- [ ] **Step 1: 실패 테스트 작성**
  - 0, 2 레이어가 첫 프리팹을 선택하는지 검증한다.
  - 3, 6 레이어가 두 번째 프리팹을 선택하는지 검증한다.
  - 7, 9 레이어가 세 번째 프리팹을 선택하는지 검증한다.
  - 동일 범위를 연속 호출하면 인스턴스를 재생성하지 않는지 검증한다.
  - 범위 밖 레이어 호출 시 기존 인스턴스가 제거되는지 검증한다.

- [ ] **Step 2: RED 확인**
  - Unity 에디터 Test Runner에서 `StageBackgroundControllerTests`를 실행한다.
  - 예상 결과는 `StageBackgroundController` 타입 부재로 인한 컴파일 실패다.

- [ ] **Step 3: 최소 구현 작성**
  - `[Serializable] BackgroundRange`에 `minRow`, `maxRow`, `prefab`을 둔다.
  - `ShowForLayer`에서 `row = layerIndex + 1`로 변환한다.
  - 첫 일치 범위를 선택하고 동일 프리팹이면 현재 인스턴스를 유지한다.
  - 변경 또는 미일치 시 현재 인스턴스를 제거한다.
  - 유효한 프리팹은 지정된 스폰 루트 또는 컨트롤러 transform 아래에 생성한다.

- [ ] **Step 4: GREEN 확인**
  - 사용자가 Unity 에디터 Test Runner에서 테스트를 실행할 수 있도록 테스트를 컴파일 가능 상태로 둔다.
  - MSBuild로 런타임 및 에디터 프로젝트의 컴파일 성공을 확인한다.

### Task 2: BattleSceneController 배경 책임 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Test: `Assets/Tests/EditMode~/StageBackgroundControllerTests.cs`

**Interfaces:**
- Consumes: `GeneratedMapNodeData nodeData`
- Produces: 전투방 진입 전에 `stageBackgroundController.ShowForLayer(nodeData.LayerIndex)` 호출

- [ ] **Step 1: 실패 테스트 작성**
  - 소스 회귀 테스트로 기존 `normalBattleBackground`, `bossBattleBackground`, `SetBattleBackground` 의존성이 제거되어야 함을 명시한다.

- [ ] **Step 2: RED 확인**
  - 현재 소스에 기존 필드와 메서드가 남아 있어 테스트가 실패하는지 확인한다.

- [ ] **Step 3: 최소 리팩터링**
  - 기존 두 배경 필드, 자동 탐색 메서드, 활성화 메서드를 제거한다.
  - `StageBackgroundController` 참조를 직렬화하고 필요 시 `battleRoom` 자식에서 찾는다.
  - `OpenBattleMap`과 `OpenBossBattle` 모두 노드의 `LayerIndex`를 전달한다.

- [ ] **Step 4: GREEN 확인**
  - 회귀 테스트와 프로젝트 컴파일을 확인한다.

### Task 3: 기존 Stage 01 오브젝트 프리팹화 및 씬 연결

**Files:**
- Create: `Assets/Project/PrefabsR/Battle/BackGround/St1_00.prefab`
- Create: `Assets/Project/PrefabsR/Battle/BackGround/St1_01.prefab`
- Create: `Assets/Project/PrefabsR/Battle/BackGround/St1_02.prefab`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: Battle 씬의 `BattleRoom/Background/Stage_01/St1_00~02` 오브젝트
- Produces: `StageBackgroundController`에 연결된 세 프리팹과 빈 스폰 루트

- [ ] **Step 1: 프리팹 변환 안전 경로 준비**
  - UnityEditor의 `PrefabUtility.SaveAsPrefabAssetAndConnect`를 사용하는 편집기 작업으로 각 기존 하이어라키를 프리팹 자산으로 저장한다.
  - `Boss` 오브젝트는 탐색, 삭제, 비활성화 또는 프리팹화하지 않는다.

- [ ] **Step 2: Stage_01 설정 연결**
  - `Stage_01`에 `StageBackgroundController`를 추가한다.
  - 범위를 1~3, 4~7, 8~10으로 설정하고 각각 `St1_00`, `St1_01`, `St1_02` 프리팹을 연결한다.
  - 기존 프리팹 인스턴스는 초기 중복 표시를 막기 위해 스폰 전 제거되도록 구성한다.

- [ ] **Step 3: 씬 검증**
  - 씬 YAML에서 세 프리팹 GUID와 컨트롤러 참조가 유효한지 확인한다.
  - `Boss` 관련 YAML이 변경되지 않았는지 diff로 확인한다.

### Task 4: 최종 검증

**Files:**
- Verify: 위에서 생성 및 수정한 파일 전체

**Interfaces:**
- Consumes: 완료된 코드, 테스트, 프리팹, 씬
- Produces: 컴파일 및 정적 검증 결과

- [ ] **Step 1: 런타임 컴파일**
  - `Assembly-CSharp.csproj`를 Restore 없이 빌드하고 오류 0개를 확인한다.

- [ ] **Step 2: 에디터 컴파일**
  - `Assembly-CSharp-Editor.csproj`를 Restore 없이 빌드하고 오류 0개를 확인한다.

- [ ] **Step 3: 변경 범위 검사**
  - `git diff --check`를 실행한다.
  - 변경 파일이 승인된 코드, 테스트, 프리팹, Battle 씬, `AI_Docs` 문서에 한정되는지 확인한다.
  - 기존 사용자 변경 파일을 건드리지 않았는지 확인한다.

### Task 5: StartRoom 및 RestRoom 공통 배경 확장

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/StageBackgroundControllerTests.cs`

**Interfaces:**
- Consumes: 대상 방 `GameObject`와 `GeneratedMapNodeData`
- Produces: `ShowRoomBackground(GameObject room, GeneratedMapNodeData nodeData)` 공통 경로

- [ ] **Step 1: 실패 테스트 작성**
  - 배틀룸 전용 `stageBackgroundController` 필드가 없어야 함을 검증한다.
  - 두 인자를 받는 `ShowRoomBackground` 메서드가 있어야 함을 검증한다.

- [ ] **Step 2: 공통 경로 구현**
  - Start, Battle, Rest 방 진입 시 대상 방 아래의 `StageBackgroundController`를 찾아 현재 `LayerIndex`를 전달한다.
  - Event/Special 방은 변경하지 않는다.

- [ ] **Step 3: 씬 설정 연결**
  - StartRoom과 RestRoom의 `Stage_01`에 같은 `St1_00~02` 프리팹과 1~3, 4~7, 8~10 범위를 연결한다.
  - 기존 중복 배경 오브젝트를 제거하고 각 방에 `SpawnRoot`를 만든다.

- [ ] **Step 4: 검증**
  - 런타임 및 에디터 프로젝트 컴파일 오류가 없는지 확인한다.
  - 세 방의 직렬화된 범위와 프리팹 GUID가 동일한지 확인한다.
