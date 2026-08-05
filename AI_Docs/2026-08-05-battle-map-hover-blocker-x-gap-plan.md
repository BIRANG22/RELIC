# Battle Map Hover Blocker And X Gap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 지도 배경의 포인터 차단을 제거하고 노드 열 간격을 100으로 축소한다.

**Architecture:** 배경은 시각 전용 Graphic으로 유지하되 레이캐스트 대상에서 제외한다. 지도 좌표의 단일 기준인 `BattleMapLayoutUtility.LayerGap`만 변경하여 생성·재배치·스크롤 계산이 같은 값을 사용하게 한다.

**Tech Stack:** Unity 6 UI, C#, NUnit EditMode tests, Unity YAML scene

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 전투 및 맵 핵심 상태를 UI에서 변경하지 않는다.

---

### Task 1: 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleHorizontalMapLayoutTests.cs`
- Modify: `Assets/Tests/EditMode~/BattleMapPanelPromotionSceneTests.cs`

- [ ] 열 간격이 정확히 100인지 검증하는 기대값을 작성한다.
- [ ] ViewPort Background 컴포넌트 블록의 `m_RaycastTarget: 0`을 검증한다.
- [ ] 기존 값 140과 씬 값 1에서 테스트가 실패하는 조건을 확인한다.

### Task 2: 최소 구현

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

- [ ] `LayerGap`을 100으로 변경한다.
- [ ] 컴포넌트 fileID `1463830777`의 `m_RaycastTarget`을 0으로 변경한다.

### Task 3: 검증

**Files:**
- Verify all files above

- [ ] `Assembly-CSharp.csproj` 컴파일을 실행한다.
- [ ] 레이아웃 상수와 씬 컴포넌트 값을 정적으로 확인한다.
- [ ] 실제 마우스 호버 검증은 열린 Unity 에디터에서 확인이 필요함을 보고한다.
