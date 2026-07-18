# Shared Upgrade Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 로비와 배틀씬이 동일한 강화 패널 프리팹을 사용하면서 씬별 강화 규칙을 유지한다.

**Architecture:** 공용 프리팹에 로비/배틀 컨트롤러를 함께 두고 컨텍스트 선택기가 활성 씬에 맞는 컨트롤러만 켠다. 씬 외부 참조는 교체 시 명시적으로 다시 연결한다.

**Tech Stack:** Unity 6, C#, uGUI, TextMeshPro, UnityEditor Prefab/Scene API

## Global Constraints

- 문서는 `AI_Docs`에만 둔다.
- 배틀과 로비 런타임 데이터는 분리한다.
- 자동 저장을 추가하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- 사용자 승인 없이 커밋하지 않는다.

### Task 1: 컨텍스트 선택기

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Shared/SkillUpgradePanelContextSelector.cs`
- Test: `Assets/Tests/EditMode~/SkillUpgradePanelContextSelectorTests.cs`

- [ ] 로비/배틀 씬 이름에 따른 모드 판정 실패 테스트를 작성한다.
- [ ] 순수 모드 판정 함수를 구현하고 테스트 컴파일을 통과시킨다.
- [ ] 활성 컨트롤러 한 개만 켜는 초기화를 구현한다.

### Task 2: 배틀 컨트롤러 버튼 자립화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/SkillUpgradePanel.cs`

- [ ] `TuningButton`과 `Cancel`을 런타임에 찾아 기존 배틀 메서드에 연결한다.
- [ ] 기존 강화 제한, 완료 연출, 자동 닫기 동작은 변경하지 않는다.

### Task 3: 공용 프리팹과 씬 교체

**Files:**
- Rename/Modify: `Assets/Project/PrefabsR/LobbySkillUpgradePanel.prefab` to `Assets/Project/PrefabsR/UpgradePanel.prefab`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Create temporarily: `Assets/Project/Editor/SharedUpgradePanelInstaller.cs`

- [ ] 로비 프리팹에 배틀 컨트롤러와 컨텍스트 선택기를 추가한다.
- [ ] 로비씬 프리팹 참조를 새 공용 경로로 유지한다.
- [ ] 배틀씬 기존 패널을 공용 프리팹 인스턴스로 교체한다.
- [ ] `RestRoomController.upgradePanel`을 새 인스턴스에 연결한다.
- [ ] 씬 저장 후 임시 설치기를 제거한다.

### Task 4: 검증

**Files:**
- Test: `Assets/Tests/EditMode~/SharedUpgradePanelPrefabTests.cs`

- [ ] C# 런타임 및 에디터 어셈블리를 컴파일한다.
- [ ] 두 씬의 프리팹 GUID가 동일한지 검사한다.
- [ ] 배틀 컨트롤러 참조와 버튼 이름을 검사한다.
- [ ] Unity가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
