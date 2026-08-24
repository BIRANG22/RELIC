# Lobby Quest Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 부트스트랩 소유 로비 퀘스트 시스템으로 튜토리얼 진행에 따른 버튼 해금을 강제한다.

**Architecture:** `LobbyQuestManager`가 저장된 `LobbyTutorialProgress`를 읽어 퀘스트 상태와 게이트 허용 여부를 계산한다. `LobbyQuestPanel`은 표시 전용이며, 로비 진입점은 `LobbyQuestGate`를 실행 전에 조회한다.

**Tech Stack:** Unity C#, MonoBehaviour, TextMeshPro, Unity UI Button, 기존 `LobbyRuntimeData`, `SaveSystem`, `DataManager`.

**Spec:** `AI_Docs/2026-08-24-lobby-quest-bootstrap-design.md`

## Global Constraints

- 문서는 `AI_Docs` 폴더 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 시도하지 않는다.
- 전투 결과를 변경하지 않고, UI/VFX/사운드는 결과 계산에 관여하지 않는다.
- 커밋, Push, PR, 브랜치 생성·전환은 수행하지 않는다.

---

### Task 1: Quest State Model

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestState.cs`
- Test: `Assets/Tests/EditMode~/LobbyQuestStateTests.cs`

**Interfaces:**
- Consumes: `Relic.Gameplay.Data.LobbyRuntimeData`, `LobbyTutorialProgress`
- Produces: `LobbyQuestState.Build(LobbyRuntimeData lobby, LobbyQuestTextConfig config)`, `LobbyQuestState.CanUseFeature(LobbyTutorialProgress current, LobbyTutorialProgress required)`

- [ ] Write failing tests for progress text and gate comparison.
- [ ] Run the EditMode test target from Unity Test Runner or compile through MSBuild if available.
- [ ] Implement `LobbyQuestTextConfig` and `LobbyQuestState`.
- [ ] Re-run verification.

### Task 2: Bootstrap Manager and Panel

**Files:**
- Modify: `Assets/Project/Scripts/Core/Bootstrap.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestManager.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestPanel.cs`
- Test: `Assets/Tests/EditMode~/LobbyQuestManagerSourceTests.cs`

**Interfaces:**
- Consumes: `LobbyQuestState.Build`
- Produces: `LobbyQuestManager.Instance`, `LobbyQuestManager.EnsureInstance()`, `LobbyQuestManager.Refresh()`, `LobbyQuestManager.CanUseFeature(LobbyTutorialProgress required)`

- [ ] Write source tests proving `Bootstrap` calls `LobbyQuestManager.EnsureInstance()` after save load.
- [ ] Implement the manager singleton and bootstrap call.
- [ ] Implement a generated canvas panel that displays current quest text in lobby scenes.
- [ ] Re-run verification.

### Task 3: Interaction Gates

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestGate.cs`
- Modify: `Assets/Project/Scripts/UI/LobbyPanelTransitionButton.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionStageSelectController.cs`
- Modify: `Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs`
- Test: `Assets/Tests/EditMode~/LobbyQuestGateSourceTests.cs`

**Interfaces:**
- Consumes: `LobbyQuestManager.CanUseFeature`
- Produces: `LobbyQuestGate.CanExecute()`, `LobbyQuestGate.ShowLockedWarning()`

- [ ] Write source tests requiring each target script to query `LobbyQuestGate`.
- [ ] Implement `LobbyQuestGate`.
- [ ] Add gate checks at the start of the three user action entry points.
- [ ] Re-run verification.

### Task 4: Tutorial Controller Cleanup

**Files:**
- Modify: `Assets/Project/Scripts/LobbyTutorialController.cs`
- Test: `Assets/Tests/EditMode~/LobbyTutorialControllerSourceTests.cs`

**Interfaces:**
- Consumes: `LobbyQuestManager.Refresh()`
- Produces: `LobbyTutorialController` without direct `QuestPanel` or `QuestText` ownership

- [ ] Write source test asserting removed quest panel references are gone.
- [ ] Remove quest panel serialized fields, movement code, and text refresh code from `LobbyTutorialController`.
- [ ] Replace direct quest refresh calls with `LobbyQuestManager.Instance?.Refresh()`.
- [ ] Re-run verification.

### Task 5: Scene Cleanup

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: `LobbyQuestGate`
- Produces: 로비 씬에서 기존 `QuestPanel` 미사용, 필요한 버튼에 게이트 연결

- [ ] Remove or deactivate the scene-owned `QuestPanel` object and clear `LobbyTutorialController` quest field references.
- [ ] Add `LobbyQuestGate` components to stage select and play entry objects with `FirstExpeditionAssigned` requirement.
- [ ] Add lower requirement gates to character setup entry points only where existing click paths need explicit enforcement.
- [ ] Verify scene YAML no longer has `m_Name: QuestPanel` under `PositionPanel`.
