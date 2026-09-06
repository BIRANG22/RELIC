# Lobby NPC Relic Shop Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lobby NPC 클릭으로 여는 전체 유물 3개 구매·리롤 패널을 구현한다.

**Architecture:** 기존 시드 기반 상품, 구매, 리롤 서비스를 유지하면서 액티브 전용 필터만 전체 유물로 확장한다. Presenter는 월드 앵커 대신 비활성 패널의 상품 컨테이너를 채우고 NPC 입력 컴포넌트가 패널 열기만 요청한다.

**Tech Stack:** Unity 6, C#, Unity UI, TextMeshPro, NUnit EditMode 테스트

## Global Constraints

- UI는 블루더스티움과 보유 유물 상태를 직접 계산하지 않고 서비스 결과를 표시한다.
- 상품 랜덤은 기존 seed 기반 `SeededLobbyRelicShopRandom`을 유지한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
- 문서는 `AI_Docs`에만 작성하고 커밋/PR은 별도 허락 전에는 진행하지 않는다.

---

### Task 1: 전체 유물 상품과 구매 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicPurchaseService.cs`
- Test: `Assets/Tests/EditMode~/LobbyRelicShopAllRelicTests.cs`

**Interfaces:**
- Consumes: `RelicDatabase`, `LobbyRuntimeData`, `LobbyRelicPricePolicy`
- Produces: 액티브/패시브를 모두 허용하는 기존 `BuildOffers`와 `Execute` API

- [ ] 액티브와 패시브가 후보에 포함되고 패시브 구매가 성공하는 테스트를 먼저 작성한다.
- [ ] 액티브 전용 조건을 제거하는 최소 변경을 적용한다.
- [ ] 런타임 어셈블리 빌드로 컴파일을 확인한다.

### Task 2: 패널형 Presenter와 NPC 입력

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferButtonUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicRefreshButtonUI.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopNpcInteraction.cs`

**Interfaces:**
- Produces: `LobbyRelicShopPresenter.Open()`, `Close()` 및 NPC의 `OnMouseDown()` 연결

- [ ] Presenter가 패널 컨테이너 안에 상품 3개와 리롤 버튼을 생성하도록 변경한다.
- [ ] 패널 활성화 시 상품과 블루더스티움 표시를 갱신한다.
- [ ] 닫기 버튼에 `Close()`를 연결한다.
- [ ] NPC 클릭 컴포넌트가 `Open()`만 호출하도록 구현한다.

### Task 3: Lobby 씬 패널 구성

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: 기존 Lobby Canvas, `Character/npc`, 기존 리롤 및 블루더스티움 아이콘
- Produces: 비활성 `RelicShopPanel`, 상품 컨테이너, 닫기 버튼, NPC 클릭 참조

- [ ] 기존 Presenter의 월드 앵커 참조를 패널/컨테이너/닫기 버튼 참조로 교체한다.
- [ ] 중앙 반투명 패널과 가로 상품 컨테이너를 배치한다.
- [ ] NPC에 `Collider2D`와 `LobbyRelicShopNpcInteraction`을 추가한다.
- [ ] 씬 시작 시 패널이 비활성 상태인지 확인한다.

### Task 4: 검증

**Files:**
- Verify: 위 코드와 Lobby 씬

**Interfaces:**
- Produces: 컴파일 및 수동 확인 가능한 NPC 상점

- [ ] Assembly-CSharp와 Assembly-CSharp-Editor 빌드를 실행한다.
- [ ] `git diff --check`, 씬 FileID 중복, NPC와 패널 참조를 검사한다.
- [ ] Unity에서 NPC 클릭, 닫기, 액티브/패시브 구매, 잔액 차감, 리롤을 확인할 체크리스트를 보고한다.
