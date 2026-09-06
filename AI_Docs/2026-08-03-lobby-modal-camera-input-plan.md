# Lobby Modal Camera And Input Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development while implementing each task.

**Goal:** 로비 위치 패널이 어떤 진입 경로로 열려도 카메라 줌을 실행하고, 열린 동안 뒤 UI와 다른 위치 패널 진입을 차단한다.

**Architecture:** `PanelCameraMover`가 대상 패널 활성 상태를 관찰하여 카메라 이동과 투명 Raycast 차단막을 함께 관리한다. `LobbyPanelTransitionButton`은 다른 Mover의 대상 패널 활성 여부를 확인하여 중복 위치 패널 열기를 거부한다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 변경은 수행하지 않는다.

---

### Task 1: 외부 패널 활성화 카메라 이동

**Files:**
- Modify: `Assets/Project/Scripts/Art/PanelCameraMover.cs`
- Test: `Assets/Tests/EditMode~/PanelCameraMoverTests.cs`

**Interfaces:**
- Consumes: `targetPanel.activeInHierarchy`, 기존 `OpenPanel()`
- Produces: 외부 비활성→활성 전환 시 한 번 실행되는 카메라 이동

- [ ] 외부에서 `targetPanel.SetActive(true)` 후 한 프레임에 카메라가 목표 위치로 이동하는 실패 테스트를 작성한다.
- [ ] 현재 구현에서 패널만 활성화되고 카메라 위치가 유지되는 실패 원인을 확인한다.
- [ ] `DetectPanelState()`에 열림 에지 처리를 추가한다.
- [ ] 카메라 이동 테스트와 기존 복귀 테스트를 검증한다.

### Task 2: 전체 Canvas 입력 차단막

**Files:**
- Modify: `Assets/Project/Scripts/Art/PanelCameraMover.cs`
- Test: `Assets/Tests/EditMode~/PanelCameraMoverTests.cs`

**Interfaces:**
- Produces: `targetPanel` 바로 뒤의 투명 전체 부모 Rect `Image`

- [ ] 패널 열림 시 투명 Image가 부모 Rect 전체를 덮고 패널 바로 뒤에 배치되는 실패 테스트를 작성한다.
- [ ] 패널 닫힘 시 차단막이 비활성화되는 실패 테스트를 작성한다.
- [ ] 패널 활성 상태가 기존 월드 입력 차단 상태에 반영되고 닫힘 시 해제되는 실패 테스트를 작성한다.
- [ ] 복귀 중 재오픈과 Mover 재활성화 시 줌·차단막이 복구되는 실패 테스트를 작성한다.
- [ ] UI 차단막과 owner 충돌 없는 전역 활성 패널 조회를 최소 구현한다.
- [ ] 입력 차단막 테스트와 카메라 테스트를 검증한다.

### Task 3: 다른 위치 패널 열기 거부

**Files:**
- Modify: `Assets/Project/Scripts/UI/LobbyPanelTransitionButton.cs`
- Test: `Assets/Tests/EditMode~/LobbyPanelTransitionButtonTests.cs`

**Interfaces:**
- Consumes: `PanelCameraMover.IsAnotherTargetPanelOpen(GameObject requestedPanel)`
- Produces: 다른 위치 패널이 활성 상태일 때 `Execute()` 조기 종료

- [ ] 첫 번째 Mover 대상 패널이 열린 상태에서 두 번째 `Execute()`가 패널을 열지 않는 실패 테스트를 작성한다.
- [ ] 활성 Mover 대상 패널 조회 API와 `Execute()` 가드를 구현한다.
- [ ] 닫기 동작과 같은 패널 재호출은 차단하지 않는지 검증한다.

### Task 4: 컴파일 및 회귀 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`

- [ ] 런타임 및 Editor 프로젝트를 빌드하여 컴파일 오류가 없는지 확인한다.
- [ ] Unity Test Runner 실행이 필요한 테스트 항목을 완료 보고에 분리한다.
- [ ] 변경 파일과 diff를 검토하여 승인 범위 밖 변경이 없는지 확인한다.
