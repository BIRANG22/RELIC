# Lobby Skill Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 배틀 씬을 수정하지 않고 로비에서 BlueDustium 기반 무제한 누적 가격 스킬 강화를 제공한다.

**Architecture:** 로비 전용 서비스가 ID와 슬롯 정보만 받아 런타임 상태를 변경하고, UI는 서비스 결과를 표시한다. 배틀 UI 계층은 에디터에서 복사해 별도 프리팹으로 저장하며 기존 배틀 오브젝트와 스크립트는 수정하지 않는다.

**Tech Stack:** Unity 6, C#, uGUI, TMPro, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 배틀 씬과 기존 배틀 업그레이드 패널은 수정하지 않는다.
- 자동 저장을 추가하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- 커밋과 PR은 별도 승인 없이 만들지 않는다.

---

### Task 1: 가격 및 강화 서비스

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillUpgrade/LobbySkillUpgradeService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeData.cs`
- Test: `Assets/Tests/EditMode~/LobbySkillUpgradeServiceTests.cs`

**Interfaces:**
- Produces: `LobbySkillUpgradePricePolicy.GetPrice(int)`, `LobbySkillUpgradeService.Execute(...)`, `LobbySkillUpgradeResult`

- [ ] 실패 테스트로 가격 100/150/200, 잔액 부족 무변경, 캐릭터 슬롯 강화, 인벤토리 강화 동작을 정의한다.
- [ ] `LobbyRuntimeData.LobbySkillUpgradeCount`를 추가한다.
- [ ] 성공 시에만 재화 차감, 스킬 ID 교체, 횟수 증가가 일어나도록 구현한다.
- [ ] C# 빌드로 테스트와 런타임 코드를 컴파일한다.

### Task 2: 로비 강화 패널 UI

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillUpgrade/LobbySkillUpgradePanelUI.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillUpgrade/LobbySkillUpgradeOpenButton.cs`
- Reuse: `Assets/Project/PrefabsR/RestRoom/SkillUpgradeIcon.prefab`

**Interfaces:**
- Consumes: `LobbySkillUpgradeService.Execute(...)`
- Produces: `Open()`, `Close()`, `TuneSelectedSkill()`, `Refresh()`

- [ ] 현재 파티와 로비 스킬 인벤토리에서 강화 가능한 스킬 요청을 만든다.
- [ ] 기존 아이콘 프리팹을 생성하고 선택, 호버, 강화 버튼을 연결한다.
- [ ] 현재 가격과 BlueDustium을 표시하고 성공 후 패널을 닫지 않은 채 목록과 가격을 갱신한다.
- [ ] 별도 닫기 버튼만 `Close()`를 호출하게 한다.

### Task 3: 프리팹과 로비 씬 배치

**Files:**
- Create: `Assets/Project/PrefabsR/LobbySkillUpgradePanel.prefab`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Temporary Create/Delete: `Assets/Project/Editor/LobbySkillUpgradePrefabInstaller.cs`
- Test: `Assets/Tests/EditMode~/LobbySkillUpgradeSceneTests.cs`

**Interfaces:**
- Consumes: `LobbySkillUpgradePanelUI.Open()` and `Close()`

- [ ] 배틀 씬을 읽기 전용으로 열어 `UpgradePanel` UI 계층을 복사하고 로비 전용 프리팹으로 저장한다.
- [ ] 로비 Canvas 아래에 비활성 프리팹 인스턴스를 배치한다.
- [ ] `PositionPanel` 아래 X 약 730 위치에 강화 버튼을 만들고 `Open()`을 연결한다.
- [ ] 패널 내부 닫기 버튼을 만들고 `Close()`를 연결한다.
- [ ] 배틀 씬 파일 해시가 변하지 않았는지 확인한다.

### Task 4: 최종 검증

**Files:**
- Verify: all files above

- [ ] 프리팹 누락 스크립트와 0 GUID 참조가 없는지 검사한다.
- [ ] 런타임 및 Editor C# 프로젝트를 빌드한다.
- [ ] Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않고 테스트 코드 컴파일을 확인한다.
- [ ] 자동 저장 호출이 새 코드에 없는지 정적 검사한다.
