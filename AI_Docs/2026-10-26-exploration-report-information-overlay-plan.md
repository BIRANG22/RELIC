# ExplorationReport Information Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 정보 아이콘을 초상 마스크에서 분리하고 호버·클릭 정보창을 아이콘 오른쪽 아래에 표시한다.

**Architecture:** 고정 UI 배치는 `Battle.unity`에 유지하되 Row 레이아웃 자식인 Portrait Container 아래에 마스크 Viewport와 아이콘을 형제로 둔다. 정보창 위치 계산은 아이콘 우하단 기준점을 받아 패널 경계 안으로 보정하는 순수 계산 메서드로 유지한다.

**Tech Stack:** Unity UI, C#, Unity Scene YAML, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-26-exploration-report-information-overlay-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 BatchMode 테스트는 수행하지 않는다.
- 전투 핵심 로직과 멀티플레이 동기화 경계는 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 세 Row 모두 Portrait Container가 레이아웃 자식이고 아이콘과 Viewport가 그 아래 형제인지 씬 YAML 테스트로 확인한다.
- 런타임 레이아웃 재계산 때 Container 단위로 아이콘과 Viewport가 함께 이동할 수 있는지 계층과 RectTransform을 확인한다.
- 정보창이 일반 위치에서 아이콘 우하단에 놓이는지 좌표 테스트로 확인한다.
- 우측·하단 경계에서 정보창이 패널 밖으로 벗어나지 않는지 좌표 테스트로 확인한다.
- 기존 호버·클릭 소유권 및 핀 동작에 사용되는 참조가 유지되는지 소스·씬 테스트로 확인한다.

---

### Task 1: 우하단 정보창 좌표 계산

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `RectTransform source`, 패널 경계 및 정보창 크기/피벗
- Produces: `CalculateStatisticsOverlayPosition(Vector2 sourceBottomRight, Rect boundary, Vector2 overlaySize, Vector2 pivot, Vector2 gap)`

- [ ] **Step 1:** 일반 위치와 우측·하단 경계 보정을 검증하는 실패 테스트를 작성한다.
- [ ] **Step 2:** Editor 테스트 어셈블리를 빌드해 기존 중앙 정렬 구현에서 실패함을 확인한다.
- [ ] **Step 3:** 아이콘 우하단과 12px 간격을 사용하도록 좌표 계산 및 호출부를 최소 수정한다.
- [ ] **Step 4:** Editor 테스트 어셈블리 빌드로 통과를 확인한다.

### Task 2: 정보 아이콘을 마스크 계층에서 분리

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: 세 Row/Viewport/Icon/StatisticsOverlayAnchor의 기존 fileID
- Produces: Row 직계 Portrait Container, Container 아래의 Viewport/아이콘 형제 구조, 유지된 `40x40` 배치와 Anchor 참조

- [ ] **Step 1:** 세 Container와 아이콘/Viewport 형제 계층을 검증하는 실패 테스트를 작성한다.
- [ ] **Step 2:** Editor 테스트 어셈블리를 빌드해 기존 마스크 자식 구조에서 실패함을 확인한다.
- [ ] **Step 3:** 씬 YAML에 세 Portrait Container를 추가하고 Viewport와 아이콘을 형제로 재배치한다.
- [ ] **Step 4:** Editor 테스트 어셈블리와 런타임 어셈블리를 빌드해 통과를 확인한다.
- [ ] **Step 5:** `git diff --check`와 변경 범위 검토로 YAML 참조 및 불필요한 변경 여부를 확인한다.
