# Battle Map Sticky Node Info And Single Line Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 최근 호버 노드 정보를 유지하고 지도 최초 표시를 현재 노드로 초기화하며 라인을 단일 회전 이미지 방식으로 복구한다.

**Architecture:** `BattleMapPanel`이 패널을 열 때 현재 노드와 아이콘을 Presenter에 전달하고 호버 종료 콜백은 연결하지 않는다. `MapLineView`는 단일 스프라이트를 거리와 각도로 배치한다.

**Tech Stack:** Unity 6 UI, C#, NUnit EditMode tests, Unity YAML prefab

## Global Constraints

- 문서는 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity batchmode 테스트를 실행하지 않는다.
- UI는 맵 및 전투 상태를 변경하지 않는다.

---

### Task 1: 단일 라인 테스트와 구현

**Files:**
- Modify: `Assets/Tests/EditMode~/MapLineViewTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapLineView.cs`
- Modify: `Assets/Project/PrefabsR/Map/LinePrefab.prefab`

- [ ] 100x40 연결의 중심, 인셋 길이와 각도를 검증하는 테스트를 작성한다.
- [ ] 방향별 기존 구현에서 테스트가 실패하는 조건을 확인한다.
- [ ] 단일 스프라이트 거리/회전 방식으로 구현한다.
- [ ] LinePrefab에서 방향별 참조를 제거한다.

### Task 2: 최근 정보 유지와 현재 노드 초기화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Tests/EditMode~/BattleMapNodeInfoPresenterTests.cs`

- [ ] Presenter가 새 Show 호출 전까지 표시값을 유지하는 테스트를 작성한다.
- [ ] 지도 Open에서 현재 노드 또는 시작 노드를 Show하도록 구현한다.
- [ ] MapViewSpawner에 호버 종료 콜백을 전달하지 않도록 변경한다.

### Task 3: 검증

**Files:**
- Verify all files above

- [ ] 런타임 어셈블리를 컴파일한다.
- [ ] LinePrefab의 스프라이트 참조와 BattleMapPanel의 호버 종료 연결 제거를 확인한다.
- [ ] Unity 에디터에서의 실제 표시 검증이 필요함을 보고한다.
