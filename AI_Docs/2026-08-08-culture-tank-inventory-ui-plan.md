# CultureTankPanel Inventory UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** CultureTankPanel 안에 Bag 스타일 8칸 소유 아이템 UI를 만들고 선택한 배양조 행에 아이템을 투입한다.

**Architecture:** 상태 변경은 기존 CultureTankResearchService를 유지하고, Presenter는 씬에 배치된 BattleBagItemSlotUI를 표시·선택하는 역할만 맡는다. 일반 BagPanel의 폐기·툴팁 기능은 복제하지 않는다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit, Unity scene YAML

## Global Constraints

- 문서는 AI_Docs에만 작성한다.
- 테스트는 Assets/Tests/EditMode~/에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 기존 BagPanel과 CultureTank 조합 데이터는 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

---

### Task 1: CultureTank 인벤토리 계약 테스트

**Files:**
- Modify: Assets/Tests/EditMode~/LobbyCultureTankPanelTests.cs

**Interfaces:**
- Consumes: LobbyCultureTankPanelPresenter.CanSelectInventoryItem(bool, bool, bool)
- Produces: 슬롯 배치와 선택 가능 조건의 회귀 계약

- [ ] **Step 1:** PreviewScene에서 Inventory/SlotRoot와 BattleBagItemSlotUI 8개를 요구하는 테스트를 추가한다.
- [ ] **Step 2:** 툴팁·버리기·BattleBagPanelUI가 배양조 내부에 없음을 검사한다.
- [ ] **Step 3:** 행 선택, 변경 권한, 완성 결과 유무에 따른 선택 가능 조건을 검사한다.
- [ ] **Step 4:** 에디터 프로젝트 빌드로 새 API 부재 실패를 확인한다.

### Task 2: Presenter 슬롯 처리

**Files:**
- Modify: Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankPanelPresenter.cs

**Interfaces:**
- Consumes: LobbyRuntimeData.BagItemIds, BattleBagItemSlotUI.Setup, CultureTankResearchService.TryPlaceIngredient
- Produces: CanSelectInventoryItem과 배양조 전용 슬롯 클릭 흐름

- [ ] **Step 1:** 기존 임시 Image/Button 래퍼를 BattleBagItemSlotUI 목록으로 교체한다.
- [ ] **Step 2:** Inventory/SlotRoot를 자동 바인딩한다.
- [ ] **Step 3:** 선택 조건을 적용해 행 선택 전 슬롯 클릭을 차단한다.
- [ ] **Step 4:** 슬롯 클릭 성공 후 저장·공유·전체 UI 갱신을 수행한다.

### Task 3: 씬 UI 배치와 검증

**Files:**
- Modify: Assets/Project/Scenes/YDM/Lobby.unity

**Interfaces:**
- Consumes: 복구된 BagPanel 슬롯의 시각 구조
- Produces: CultureTankPanel 내부의 독립적인 8칸 슬롯 UI

- [ ] **Step 1:** CultureTankPanel 내부에 Inventory/SlotRoot 계층을 추가한다.
- [ ] **Step 2:** 슬롯 8개를 Bag 스타일로 배치하고 BattleBagItemSlotUI를 연결한다.
- [ ] **Step 3:** Presenter의 inventoryItemRoot를 새 SlotRoot에 연결한다.
- [ ] **Step 4:** 씬 구조 검사와 런타임·에디터 빌드를 실행한다.
