# Bootstrap Tutorial Quest Manager Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development for each behavior. Project rule override: documents live in `AI_Docs`.

**Goal:** Bootstrap에서 유지되는 튜토리얼 퀘스트 매니저와 공용 퀘스트 패널 기반을 만든다.

**Architecture:** `QuestManager`는 순수 데이터 서비스, `QuestManagerHost`는 Bootstrap 수명주기 연결, `QuestPanelPresenter`는 표시 전용으로 분리한다. `LobbyRuntimeData`에 저장 필드를 추가하고 기존 SaveSystem 경로를 재사용한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, Unity YAML prefab/scene.

**Spec:** `AI_Docs/2026-08-23-bootstrap-tutorial-quest-manager-design.md`

## Global Constraints

- `Assets/Project/Scenes/YDM/Lobby.unity`를 수정하지 않는다.
- `Assets/Project/Scenes/YDM/Battle.unity`를 수정하지 않는다.
- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 커밋, Push, PR은 사용자 승인 없이 수행하지 않는다.

---

## Task 1: Quest Runtime State

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeStore.cs`
- Modify: `Assets/Project/Scripts/Core/SaveSystem.cs`
- Test: `Assets/Tests/EditMode~/QuestManagerTests.cs`

**Interfaces:**
- Produces: `LobbyRuntimeData.ActiveQuestId`, `CompletedQuestIds`, `UnlockedSystemIds`

- [ ] Write failing tests for normalization and save snapshot persistence.
- [ ] Run compile/test command and confirm failure.
- [ ] Add fields and normalization.
- [ ] Run verification.

## Task 2: QuestManager Behavior

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Managers/QuestManager.cs`
- Test: `Assets/Tests/EditMode~/QuestManagerTests.cs`

**Interfaces:**
- Produces: `QuestActionId`, `QuestActionGateResult`, `QuestDisplayState`, `QuestManager.Initialize`, `CanPerformAction`, `MarkActionCompleted`, `GetCurrentDisplayState`

- [ ] Write failing tests for locked first-open action and unlock persistence.
- [ ] Run compile/test command and confirm failure.
- [ ] Implement minimal manager behavior.
- [ ] Run verification.

## Task 3: Bootstrap Host And Panel Presenter

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Quest/QuestManagerHost.cs`
- Create: `Assets/Project/Scripts/Gameplay/Quest/QuestPanelPresenter.cs`
- Create: `Assets/Project/PrefabsR/QuestPanel.prefab`
- Modify: `Assets/Project/Scenes/YDM/Bootstrap.unity`
- Test: `Assets/Tests/EditMode~/QuestManagerBootstrapTests.cs`

**Interfaces:**
- Consumes: `QuestManager.GetCurrentDisplayState()`
- Produces: `QuestManagerHost.Instance`, `QuestManagerHost.Manager`, `QuestPanelPresenter.Show`

- [ ] Write failing compile tests for Host and Presenter APIs.
- [ ] Run compile/test command and confirm failure.
- [ ] Add Host, Presenter, prefab, Bootstrap scene instance.
- [ ] Run verification and confirm Lobby/Battle scenes unchanged.
