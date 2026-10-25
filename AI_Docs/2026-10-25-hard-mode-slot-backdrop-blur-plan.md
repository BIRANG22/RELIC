# HardModeSlot 외곽 배경 블러 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 아이콘 블러를 유지하면서 각 `HardModeSlot` 전체 뒤에 더 큰 붉은 외곽 블러를 표시한다.

**Architecture:** 새 블러를 슬롯 자식이 아닌 `ExplorationReportSelectedErosionSlots`의 형제로 생성한다. 패널 UI가 슬롯과 블러의 위치·스케일·활성 상태를 함께 갱신하고, 블러를 대응 슬롯의 바로 앞 sibling으로 정렬한다.

**Tech Stack:** Unity 6, C#, UGUI `Image`, Unity YAML prefab/scene, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-25-hard-mode-slot-backdrop-blur-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래 기존 테스트 파일에 추가한다.
- 기존 `HardModeRedBlur` 아이콘 블러를 유지한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 조작은 하지 않는다.

## Review Focus

- 빈 침식 슬롯이면 대응 외곽 블러도 비활성화되는지 테스트한다.
- 슬롯이 재배치될 때 블러가 같은 위치와 스케일을 받는지 테스트한다.
- 블러가 슬롯의 자식이 아니라 같은 루트의 형제로 생성되는지 테스트한다.
- 블러가 항상 대응 슬롯 바로 앞 sibling으로 이동하는지 테스트한다.
- 블러가 입력을 가로채지 않도록 `raycastTarget`이 꺼져 있는지 테스트한다.

---

### Task 1: HardModeSlot 외곽 블러 에셋 및 바인딩

**Files:**
- Create: `Assets/Project/PrefabsR/ExplorationResultHardModeBackdropBlur.prefab`
- Create: `Assets/Project/PrefabsR/ExplorationResultHardModeBackdropBlur.prefab.meta`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `ApplySlotLayout(RectTransform, int, int, float, float, float)`과 `hardModeSlotRoot`
- Produces: `EnsureHardModeBackdropBlurCount(int)` 및 `ApplyHardModeBackdropLayout(RectTransform, int)`

- [ ] **Step 1: 실패하는 에셋/소스 테스트 작성**

`HardModeSlots_PlaceBackdropBlurBehindWholeSlot`에서 새 프리팹의 이름, 크기, Sprite, 붉은 색상, `raycastTarget = false`, 씬의 직렬화 참조, 형제 생성, 레이아웃 및 활성 상태 동기화, 대응 슬롯 직전 sibling 배치를 검증한다.

- [ ] **Step 2: 테스트가 실패함을 확인**

Run: 새 프리팹과 소스 표식을 검사하는 PowerShell 검증
Expected: 새 프리팹이 없어 실패

- [ ] **Step 3: 최소 구현 추가**

`150 x 150` RectTransform, 기존 글로우 Sprite, 붉은 반투명 Image를 가진 전용 프리팹을 추가한다. 패널에 `Image hardModeBackdropBlurPrefab`과 블러 리스트를 추가하고, 루트 아래 형제로 최대 슬롯 수만큼 생성해 슬롯과 같은 위치·스케일·활성 상태를 적용한 뒤 슬롯 바로 앞에 정렬한다.

- [ ] **Step 4: 검증 통과 확인**

Run: 구조 검증, `Assembly-CSharp-Editor.csproj` 컴파일, `git diff --check`
Expected: 구조 검증 통과, 컴파일 오류 0, whitespace 오류 없음

- [ ] **Step 5: 커밋 생략**

사용자 승인 전 커밋하지 않는다.
