# Exploration Result Character Portrait Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 탐사 정산 카드의 캐릭터 이미지를 2배 확대·클리핑하고 정보 아이콘을 원형 버튼으로 옮기면서 정보창 위치를 유지한다.

**Architecture:** 씬에 명시적인 `PortraitViewport`와 `StatisticsOverlayAnchor`를 배치한다. `ExplorationResultCharacterRowUI`는 상호작용 대상인 정보 아이콘과 정보창 배치 기준점을 서로 다른 serialized reference로 사용한다.

**Tech Stack:** Unity 6 UI, RectTransform, Image, RectMask2D, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-24-exploration-result-character-portrait-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
- 전투 핵심 상태와 멀티플레이 경계는 변경하지 않는다.

## Review Focus

- Portrait가 Viewport 밖으로 넘을 때 RectMask2D로 잘리는지 확인한다.
- 정보 아이콘만 호버·클릭 영역이 되는지 확인한다.
- 아이콘 이동 후에도 별도 Anchor를 통해 정보창 위치가 유지되는지 확인한다.
- 캐릭터 이미지가 없는 경우 기존처럼 Image가 비활성화되는지 확인한다.
- 세 캐릭터 카드에 동일한 계층과 설정이 적용되는지 확인한다.

---

### Task 1: 씬 레이아웃 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `Battle.unity`의 캐릭터 행 GameObject 및 컴포넌트 문서
- Produces: Portrait Viewport와 정보 아이콘/Anchor 구성을 고정하는 회귀 테스트

- [ ] `CharacterReport_UsesMaskedDoubleScalePortraitAndCornerInformationButton` 테스트를 추가한다.
- [ ] 정적 검사로 기존 씬에서 실패하는지 확인한다.

### Task 2: 캐릭터 행 상호작용 기준점 분리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs`

**Interfaces:**
- Consumes: `informationIcon`, `statisticsOverlayAnchor`
- Produces: 아이콘은 이벤트를 받고 Anchor는 `ShowStatisticsOverlay` 및 `ToggleStatisticsOverlay`의 source가 된다.

- [ ] `statisticsOverlayAnchor` serialized reference와 자동 연결을 추가한다.
- [ ] 호버와 클릭에서 정보창 source로 Anchor를 전달한다.
- [ ] 런타임 코드 컴파일을 확인한다.

### Task 3: 세 캐릭터 카드 씬 구성

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: Task 2의 `statisticsOverlayAnchor`
- Produces: 세 카드의 `250 x 90` RectMask2D Viewport, 2배 Portrait, `40 x 40` 원형 정보 버튼, 기존 위치 Anchor

- [ ] 각 Portrait를 새 Viewport 자식으로 이동하고 `2 x 2` 배율을 적용한다.
- [ ] 각 정보 아이콘을 왼쪽 아래 원형 버튼으로 구성한다.
- [ ] 기존 정보 아이콘 위치에 `StatisticsOverlayAnchor`를 배치하고 serialized reference를 연결한다.
- [ ] 정적 회귀 검사와 씬 FileID 무결성을 확인한다.
- [ ] `Assembly-CSharp`와 `Assembly-CSharp-Editor`를 컴파일한다.
