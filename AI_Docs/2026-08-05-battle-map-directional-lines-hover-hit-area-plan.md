# Battle Map Directional Lines And Hover Hit Area Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 방향별 꺾인 라인을 정확히 표시하고 모든 지도 노드의 호버 정보 입력을 안정적으로 처리한다.

**Architecture:** `MapLineView`가 좌표로부터 순수한 레이아웃/방향 판정을 만들고 연결된 스프라이트를 적용한다. 노드 호버는 클릭 Button과 분리된 `HoverHitArea` 자식이 담당하며 `MapViewSpawner`가 자식 릴레이에 콜백을 주입한다.

**Tech Stack:** Unity 6 UI, C#, NUnit EditMode tests, Unity YAML prefabs

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- UI 변경은 맵/전투 핵심 상태를 직접 변경하지 않는다.

---

### Task 1: 방향별 라인 판정과 배치

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapLineView.cs`
- Modify: `Assets/Project/PrefabsR/Map/LinePrefab.prefab`
- Modify: `Assets/Tests/EditMode~/MapLineViewTests.cs`

**Interfaces:**
- Produces: `MapLineLayout MapLineView.CalculateLayout(Vector2 from, Vector2 to)`
- Produces: `void MapLineView.Setup(Vector2 from, Vector2 to)`

- [ ] 방향별 종류, 플립과 RectTransform 영역을 검증하는 실패 테스트를 작성한다.
- [ ] 기존 구현이 스프라이트 종류를 제공하지 못해 테스트가 실패함을 확인한다.
- [ ] `MapLineLayout` 계산과 3개 스프라이트 적용을 구현한다.
- [ ] LinePrefab에 세 스프라이트를 연결하고 레이캐스트를 끈다.
- [ ] 컴파일과 직렬화 참조를 검증한다.

### Task 2: 클릭과 분리된 호버 입력

**Files:**
- Modify: `Assets/Project/PrefabsR/Map/NodePrefab.prefab`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapNodeHoverRelay.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapViewSpawner.cs`
- Modify: `Assets/Tests/EditMode~/MapNodeHoverRelayTests.cs`

**Interfaces:**
- Consumes: `MapNodeHoverRelay.Configure(GeneratedMapNodeData, Sprite, Action<GeneratedMapNodeData, Sprite>, Action)`
- Produces: `HoverHitArea` 자식의 독립적인 포인터 진입/종료 처리

- [ ] 자식 릴레이의 진입·종료 콜백과 아이콘 확대를 검증하는 실패 테스트를 작성한다.
- [ ] 생성기가 루트 전용 검색을 사용해 자식 릴레이를 놓치는 상태를 확인한다.
- [ ] 생성기의 검색을 `GetComponentInChildren<MapNodeHoverRelay>(true)`로 변경한다.
- [ ] NodePrefab에 투명한 전면 HoverHitArea와 아이콘 참조가 연결된 릴레이를 배치한다.
- [ ] 컴파일과 프리팹 참조를 검증한다.

### Task 3: 최종 검증

**Files:**
- Verify all files above

- [ ] `Assembly-CSharp.csproj`를 빌드해 런타임 컴파일 오류가 없음을 확인한다.
- [ ] 세 스프라이트 GUID, HoverHitArea 레이캐스트, 릴레이 자식 검색을 정적으로 확인한다.
- [ ] Unity Test Runner와 실제 마우스 호버 확인이 필요한 항목을 완료 보고에 명시한다.
