# Culture Tank Panel Implementation Plan

Goal: Researcher 오브젝트를 클릭하면 배양조 패널이 열리고, 패널 안의 배양조 버튼이 기존 배양/완료 수령 기능을 그대로 실행하게 만든다.

Architecture:
- 기존 배양 기능은 `LobbyCultureTankController`에 유지한다.
- 월드 배양조 직접 클릭 진입점은 비활성화하고, 패널이 호출할 public interaction 메서드를 추가한다.
- `Researcher` 오브젝트는 런타임 바인더가 찾아 클릭 컴포넌트를 붙인다.
- 패널은 런타임에 Canvas 아래 생성하며 닫기 버튼과 배양조 목록을 가진다.

Files:
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankAutoBinder.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankPanelPresenter.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyResearcherCultureTankInteraction.cs`
- Test: `Assets/Tests/EditMode~/LobbyCultureTankPanelTests.cs`

Tasks:
- Add tests for panel interaction entry points and auto-binder source rules.
- Expose `LobbyCultureTankController.Interact()` and panel status helpers.
- Add dynamic culture tank panel with close button and tank rows.
- Add Researcher click interaction.
- Extend auto-binder to attach Researcher interaction at Lobby scene load.
- Verify with MSBuild. Unity batchmode tests are not run because the editor is assumed open.
