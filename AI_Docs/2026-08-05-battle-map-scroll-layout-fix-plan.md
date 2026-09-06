# Battle Map Scroll Layout Fix Implementation Plan

> **For agentic workers:** 승인된 현재 작업 공간에서 TDD 순서로 구현한다.

**Goal:** 배틀 지도 패널의 위치를 고정하고 3행 축소형 지도를 현재 노드부터 가로 드래그할 수 있게 한다.

**Architecture:** 맵 데이터 위치 계산은 `BattleMapLayoutUtility`와 `ProceduralMapGenerator`가 담당하고, ScrollRect 표시 범위 계산은 `BattleMapPanel`이 담당한다. 씬에서는 지도 렌더링 루트를 Content 아래에 배치해 Unity ScrollRect 표준 동작을 사용한다.

**Tech Stack:** Unity 6, C#, uGUI ScrollRect, NUnit EditMode

## Global Constraints

- 기존 MapPanel·MapArea·Viewport의 수동 RectTransform 화면 배치는 유지한다.
- DebugBattle 씬은 수정하지 않는다.
- 랜덤 생성은 기존 `BattleRandom`을 유지한다.
- 커밋, Push, PR은 수행하지 않는다.

### Task 1: 레이아웃 및 생성 제한

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleHorizontalMapLayoutTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`

- [ ] 3개 행의 위치가 `-75, 0, 75`이고 인접 레이어 X 차이가 140인지 실패 테스트를 작성한다.
- [ ] 생성된 모든 레이어의 노드 수가 3 이하인지 실패 테스트를 작성한다.
- [ ] 테스트가 기존 280/150 간격과 4~5개 레이어 때문에 실패함을 확인한다.
- [ ] 간격 상수와 생성 개수·고정 배열·fallback·총 개수 조건을 3개 제한에 맞게 수정한다.
- [ ] 테스트 코드와 프로덕션 코드를 컴파일한다.

### Task 2: ScrollRect Content와 포커스

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleMapSelectionUiTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

- [ ] 현재 노드 X를 왼쪽 여백에 맞추는 Content X 계산 실패 테스트를 작성한다.
- [ ] 지도 최대 X에 따라 Content 폭이 Viewport보다 커지는 실패 테스트를 작성한다.
- [ ] 테스트가 현재 다음 노드 평균 포커스와 고정 Content 폭 때문에 실패함을 확인한다.
- [ ] 계산을 테스트 가능한 정적 유틸리티로 분리하고 `BattleMapPanel`에서 적용한다.
- [ ] MapPanel의 `BattleCharacterPanelUI`를 제거하고 LineRoot/NodeRoot를 Content 아래로 이동한다.
- [ ] ScrollRect 가로 드래그, Content 피벗·앵커, 렌더 순서를 연결한다.

### Task 3: 노드 프리팹 축소 및 종합 검증

**Files:**
- Modify: `Assets/Project/PrefabsR/Map/NodePrefab.prefab`
- Modify: `Assets/Tests/EditMode~/BattleMapPanelPromotionSceneTests.cs`

- [ ] 프리팹 40×40과 씬 계층·컴포넌트 조건을 검사하는 회귀 테스트를 작성한다.
- [ ] 기존 80×80 및 잘못된 계층에서 실패함을 확인한다.
- [ ] 프리팹 RectTransform을 40×40으로 변경한다.
- [ ] 전체 C# 및 테스트 컴파일, 씬 YAML 참조, DebugBattle 미수정을 검증한다.
