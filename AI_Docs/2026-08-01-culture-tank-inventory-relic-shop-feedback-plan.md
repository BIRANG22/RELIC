# Culture Tank Inventory and Relic Shop Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CultureTankPanel 내부 가방 선택 연구, ESC 닫기, 유물 호버 설명·확대, 리롤 아이콘 보존을 구현한다.

**Architecture:** UI 프리젠터는 로비 런타임의 안정적인 ID를 화면 슬롯에 투영하고 기존 연구 서비스에 명령을 전달한다. 유물 상품 버튼은 호버 상태를 표현하고 프리젠터는 유물 설명 데이터를 공급한다.

**Tech Stack:** Unity 6, C#, uGUI, TextMeshPro, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
- 연구 상태 변경은 `TankId`와 `ItemId`를 사용하고 기존 호스트 권한 및 스냅샷 흐름을 유지한다.

---

### Task 1: 배양조 내부 인벤토리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankPanelPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankController.cs`
- Test: `Assets/Tests/EditMode~/LobbyCultureTankPanelTests.cs`

**Interfaces:**
- Consumes: `LobbyRuntimeData.BagItemIds`, `ItemIconDatabase`, `CultureTankResearchService.TryStartResearch`
- Produces: 빈 행 선택 후 패널 내부 슬롯 클릭으로 연구를 시작하는 UI 흐름

- [ ] 실패 테스트에 내부 슬롯의 가방 아이템 표시와 선택 콜백 동작을 작성한다.
- [ ] 현재 코드에서 테스트가 원하는 API 부재로 실패하는지 컴파일로 확인한다.
- [ ] 프리젠터에 인벤토리 루트 탐색, 슬롯 바인딩, 선택 배양조 상태, 갱신 로직을 구현한다.
- [ ] 컨트롤러에 패널이 전달한 `itemId`로 연구를 시작하는 공개 진입점을 추가한다.
- [ ] 성공·실패·빈 슬롯 테스트가 통과하도록 최소 구현을 정리한다.

### Task 2: ESC 우선 닫기

**Files:**
- Modify: `Assets/Project/Scripts/LobbyMainPanelKeyboardInputController.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/LobbyMainPanelKeyboardInputControllerTests.cs`

**Interfaces:**
- Consumes: `LobbyCultureTankPanelPresenter.Close()`
- Produces: 유물 상점 다음 우선순위로 배양조 패널 닫기

- [ ] 배양조가 활성화된 상태에서 ESC 우선 닫기가 배양조를 닫는 실패 테스트를 작성한다.
- [ ] 테스트가 배양조 참조와 분기 부재로 실패하는지 확인한다.
- [ ] 직렬화 참조, 이름 기반 자동 연결, 닫기 분기를 구현한다.
- [ ] Lobby 씬의 키보드 컨트롤러 참조를 연결한다.
- [ ] 기존 유물 상점과 침식도 우선순위 회귀 테스트를 함께 확인한다.

### Task 3: 유물 상품 호버 피드백

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferButtonUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/LobbyRelicShopPresenterTests.cs`

**Interfaces:**
- Consumes: `RelicDatabase.Get(relicId)`, `RelicData.Name`, `RelicData.EffectDesc`
- Produces: 상품 호버 진입/이탈 콜백과 1.12배 아이콘 확대, 설명 영역 표시

- [ ] 호버 시 아이콘 확대와 설명 콜백, 이탈 시 복원을 검증하는 실패 테스트를 작성한다.
- [ ] 현재 상품 버튼이 포인터 인터페이스를 제공하지 않아 실패하는지 확인한다.
- [ ] 상품 버튼에 포인터 인터페이스와 원본 스케일 캐시·복원 로직을 구현한다.
- [ ] 프리젠터에 설명 표시/숨김 로직을 구현하고 상품 바인딩에 콜백을 전달한다.
- [ ] Lobby 씬에 설명 텍스트 참조를 연결하고 회귀 테스트를 확인한다.

추가 조정: 기존 `relic_name`과 `relic_effect`를 직접 사용하고 배경 이미지는 항상 활성 상태로 유지한다. 배양조 행은 `Label`과 `StateLabel`로 이름과 상태를 분리하며, 패널 아이템 선택에는 월드 클릭 차단 검사를 적용하지 않는다.

### Task 4: 리롤 아이콘 보존

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicRefreshButtonUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/LobbyRelicShopPresenterTests.cs`

**Interfaces:**
- Consumes: 씬에 직렬화된 `RefreshIcon.sprite`
- Produces: 초기화 이후에도 동일한 스프라이트를 유지하는 리롤 버튼

- [ ] 초기화 전후 스프라이트 동일성을 검증하는 실패 테스트를 작성한다.
- [ ] 현재 `Initialize(Sprite, Action)`가 스프라이트를 덮어써 실패하는지 확인한다.
- [ ] 초기화 API를 콜백 전용으로 바꾸고 프리젠터의 아이콘 필드를 제거한다.
- [ ] 씬 직렬화 데이터에서 제거된 필드를 정리한다.
- [ ] 테스트와 두 C# 프로젝트 빌드를 실행한다.

### Task 5: 최종 검증

**Files:**
- Review: 위 변경 파일 전체

**Interfaces:**
- Consumes: Tasks 1~4 결과
- Produces: 컴파일 가능한 통합 변경

- [ ] `rg`로 배양조 슬롯, ESC 참조, 호버 콜백, 리롤 덮어쓰기 제거를 정적 확인한다.
- [ ] `Assembly-CSharp.csproj`를 MSBuild로 빌드한다.
- [ ] `Assembly-CSharp-Editor.csproj`를 MSBuild로 빌드한다.
- [ ] `git diff --check`와 `git diff --stat`을 확인한다.
- [ ] Unity Test Runner에서 실행하지 못한 EditMode 테스트를 완료 보고에 명시한다.
