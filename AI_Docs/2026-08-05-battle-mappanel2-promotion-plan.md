# Battle MapPanel2 Promotion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Battle 씬의 MapPanel2를 단일 MapPanel로 승격하고 다음 노드 선택 버튼을 내부 전용 루트에서 생성한다.

**Architecture:** 씬 fileID를 보존하여 기존 지도 컴포넌트와 자식을 새 루트로 이전한다. `BattleMapPanel`은 직렬화된 `BattleNextNodeSelectionPanel` 루트를 우선 사용하고 그 내부에 선택 버튼을 보장한다.

**Tech Stack:** Unity 6, C#, Unity YAML Scene, NUnit EditMode tests

## Global Constraints

- `Battle.unity`만 수정하고 `DebugBattle.unity`는 수정하지 않는다.
- 사용자 제작 `MapPanel2` 자식과 레이아웃을 보존한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: 씬 구조 실패 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/BattleMapPanelPromotionSceneTests.cs`

**Interfaces:**
- Consumes: `Assets/Project/Scenes/YDM/Battle.unity` 텍스트 직렬화.
- Produces: 단일 MapPanel,旧 fileID 제거, 선택 루트 내부 배치 검증.

- [ ] 현재 씬에서 `MapPanel2` 존재와旧 루트 참조 때문에 실패하는 테스트를 작성한다.
- [ ] 테스트 소스 컴파일과 현재 씬 텍스트 기준 실패 조건을 확인한다.

### Task 2: MapPanel2 승격

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Produces: GameObject `1138866861` 기반 최종 `MapPanel`, 이전된 `BattleMapPanel` fileID `511562515`.

- [ ] 새 루트의 이름·레이어·컴포넌트 목록과 자식 목록을 갱신한다.
- [ ]旧 지도 자식의 부모를 새 RectTransform fileID `1138866862`로 교체한다.
- [ ] 모든旧 MapPanel GameObject 참조를 `1138866861`로 교체한다.
- [ ]旧 GameObject/RectTransform/Image/CanvasRenderer 블록을 제거한다.
- [ ] Canvas 자식 목록에서旧 RectTransform을 제거한다.

### Task 3: 내부 선택 버튼 생성 루트

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Modify: `Assets/Tests/EditMode~/BattleMapSelectionUiTests.cs`

**Interfaces:**
- Consumes: `BattleNextNodeSelectionPanel` 직렬화 참조.
- Produces: 선택 루트 내부의 런타임 버튼 세 개.

- [ ] 빈 선택 루트를 전달했을 때 버튼 세 개가 내부에 생성되는 실패 테스트를 작성한다.
- [ ] `EnsureNextNodeSelectionPanel`이 기존 루트에도 버튼을 채우도록 구현한다.
- [ ] 씬에 `NextNodeSelectionRoot`를 만들고 `BattleMapPanel`에 연결한다.

### Task 4: 검증

**Files:**
- Verify modified files.

**Interfaces:**
- Verifies: 씬 참조 무결성, 컴파일, 패치 오류.

- [ ] `git diff --check`를 실행한다.
- [ ] 프로덕션과 테스트 소스를 MSBuild로 컴파일한다.
- [ ] 씬 텍스트 검증을 실행한다.
- [ ] Unity 에디터 수동 확인 미실행 항목을 완료 보고에 기록한다.
