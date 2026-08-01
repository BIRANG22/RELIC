# Lobby World Panel Mouse Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비의 유물상점, 침식도, 배양조가 마우스 다운이 아니라 정상적인 마우스 릴리스 클릭에서 열리도록 한다.

**Architecture:** 세 패널이 공유하는 `PanelCameraMover`의 Unity 메시지를 `OnMouseUpAsButton`으로 통일한다. 별도 입력 경로가 있는 침식도 버튼도 같은 메시지를 사용해 드래그 후 릴리스 오작동을 차단한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR을 수행하지 않는다.

---

### Task 1: 월드 패널 입력을 릴리스 클릭으로 통일

**Files:**
- Modify: `Assets/Project/Scripts/Art/PanelCameraMover.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyErosionMirrorButton.cs`
- Test: `Assets/Tests/EditMode~/PanelCameraMoverTests.cs`
- Test: `Assets/Tests/EditMode~/LobbyErosionMirrorButtonTests.cs`

**Interfaces:**
- Consumes: Unity `OnMouseUpAsButton()` 메시지
- Produces: 동일 콜라이더에서 누르고 놓을 때만 실행되는 패널 열기 입력

- [ ] **Step 1: 실패 회귀 테스트 작성**

  두 소스가 `OnMouseUpAsButton`을 포함하고 `OnMouseDown` 또는 일반 `OnMouseUp` 진입점을 포함하지 않는지 검사한다.

- [ ] **Step 2: 구현 전 실패 확인**

  소스 검사 명령에서 두 기존 진입점 때문에 실패하는지 확인한다.

- [ ] **Step 3: 최소 구현**

  `PanelCameraMover.OnMouseDown`과 `LobbyErosionMirrorButton.OnMouseUp`의 메서드 이름을 각각 `OnMouseUpAsButton`으로 변경한다.

- [ ] **Step 4: 검증**

  동일한 소스 검사와 `Assembly-CSharp`, `Assembly-CSharp-Editor` 빌드를 실행한다.

