# Lobby BagPanel 복구 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비 씬에서 삭제된 기존 `BagPanel`과 `BagButton` 계층을 다른 로비 변경을 보존한 채 원래 상태로 복구한다.

**Architecture:** 현재 씬 전체를 되돌리지 않고, Git의 기준 씬에서 Bag UI에 속한 직렬화 객체와 부모-자식 참조만 선별해 현재 씬에 병합한다. 기존 `LobbyBagScenePlacementTests`와 씬 참조 무결성 검사로 복구 결과를 확인한다.

**Tech Stack:** Unity 6 scene YAML, C#, NUnit, Git diff

## Global Constraints

- 현재 `Lobby.unity`의 Bag UI 외 사용자 변경을 덮어쓰지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래의 기존 테스트를 사용한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

---

### Task 1: 삭제 상태 재현

**Files:**
- Test: `Assets/Tests/EditMode~/LobbyBagScenePlacementTests.cs`
- Inspect: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: 로비 씬 YAML과 기존 Bag UI 배치 계약
- Produces: Bag UI가 삭제된 현재 상태에서 실패하는 구조 검사 결과

- [ ] **Step 1:** 기존 테스트가 요구하는 `BagPanel`, `BagButton`, `SlotRoot`, `BattleBagPanelUI`, 런타임 컨텍스트 참조를 확인한다.
- [ ] **Step 2:** 현재 씬에서 해당 객체가 없어서 검사가 실패하는지 확인한다.

### Task 2: Bag UI 직렬화 객체 선별 복구

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: Git 기준 씬의 Bag UI 직렬화 객체와 현재 씬의 유지 대상 변경
- Produces: 원래 fileID와 컴포넌트 연결을 유지하는 씬 배치 Bag UI

- [ ] **Step 1:** Git 기준 씬에서 Bag UI 전용 fileID `2300000001`~`2300000144` 객체 블록을 추출한다.
- [ ] **Step 2:** Bag 계층에 포함된 기존 fileID 객체와 부모 `m_Children` 연결을 식별한다.
- [ ] **Step 3:** 다른 변경을 유지하며 식별된 삭제분만 현재 씬에 병합한다.

### Task 3: 복구 검증

**Files:**
- Verify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/LobbyBagScenePlacementTests.cs`

**Interfaces:**
- Consumes: 복구된 로비 씬
- Produces: 씬 구조, YAML 참조, C# 컴파일 검증 결과

- [ ] **Step 1:** Bag UI 구조 검사를 다시 실행해 통과를 확인한다.
- [ ] **Step 2:** 씬의 모든 로컬 fileID 참조가 정의되어 있는지 검사한다.
- [ ] **Step 3:** `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] **Step 4:** 변경 diff를 검토해 Bag UI 외 로비 변경이 보존되었는지 확인한다.
