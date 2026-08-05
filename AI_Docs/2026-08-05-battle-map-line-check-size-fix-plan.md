# Battle Map Line Check Size Fix Implementation Plan

> **For agentic workers:** 승인된 현재 작업 공간에서 TDD 순서로 구현한다.

**Goal:** 지도 체크 이미지와 노드 간격을 축소하고 가로형 라인을 올바르게 렌더링하며 씬 직렬화 오류를 제거한다.

**Architecture:** 좌표 간격은 `BattleMapLayoutUtility`, 라인 Rect 계산은 `MapLineView`, 체크 이미지 크기는 `NodePrefab`, 씬 참조 무결성은 `Battle.unity`가 담당한다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode

## Global Constraints

- CheckAnimationImage는 38.4×38.4다.
- RowGap은 40이다.
- 라인 Rect는 `(거리, 두께)`이며 연결 각도를 그대로 사용한다.
- MapPanel Y는 변경하지 않는다.
- DebugBattle은 수정하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

### Task 1: 실패 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleHorizontalMapLayoutTests.cs`
- Modify: `Assets/Tests/EditMode~/BattleMapPanelPromotionSceneTests.cs`
- Create: `Assets/Tests/EditMode~/MapLineViewTests.cs`
- Create: `Assets/Tests/EditMode~/MapNodeViewCheckImageTests.cs`

- [ ] 3행 위치가 40 간격인지 검증한다.
- [ ] 수평선이 100×8이고 회전 0도인지 검증한다.
- [ ] NodePrefab 인스턴스의 CheckAnimationImage가 38.4×38.4인지 검증한다.
- [ ] MapPanel이 비활성 이동 컴포넌트 블록을 참조하는지 검증한다.
- [ ] 기존 75 간격, 세로형 라인, 96 크기, 고아 블록 상태에서 실패 조건을 확인한다.

### Task 2: 최소 구현

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapLineView.cs`
- Modify: `Assets/Project/PrefabsR/Map/NodePrefab.prefab`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

- [ ] RowGap을 40으로 변경한다.
- [ ] 라인 크기와 회전 계산을 가로형 이미지 기준으로 변경한다.
- [ ] checkImageSize를 38.4×38.4로 변경한다.
- [ ] MapPanel 컴포넌트 목록에 비활성 직렬화 블록 참조를 복구한다.
- [ ] 테스트 코드와 프로덕션 코드를 컴파일한다.

### Task 3: 최종 검증

- [ ] 씬 참조 무결성과 MapPanel Y=540을 확인한다.
- [ ] LinePrefab 이미지 Raycast 비활성 및 24:1 기본 Rect를 확인한다.
- [ ] DebugBattle 미수정과 임시 프로젝트 설정 제거를 확인한다.
- [ ] 최종 프로덕션 컴파일과 변경 파일 diff 검사를 수행한다.
