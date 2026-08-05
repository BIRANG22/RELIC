# Battle Map Party HP Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 맵 패널 캐릭터 정보에 현재 HP 비율을 나타내는 얇은 붉은 HP 바를 추가한다.

**Architecture:** 기존 `BattleMapPartyInfoPresenter`의 단일 렌더 경로에서 텍스트, 아이콘과 함께 `HpBar/Fill`을 갱신한다. 씬에는 Character1~3 각각 편집 가능한 배경/채움 Image를 직렬화한다.

**Tech Stack:** Unity 6, C#, UGUI Image, NUnit EditMode 테스트

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- UI는 전투 상태를 변경하지 않는다.

---

### Task 1: HP 비율 렌더 동작

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleMapPartyInfoPresenterTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPartyInfoPresenter.cs`

**Interfaces:**
- Consumes: `CharacterRuntimeData.CurrentHP`, `CharacterRuntimeData.MaxHP`
- Produces: `CharacterN/HpBar/Fill`의 `Image.fillAmount`

- [ ] **Step 1: 실패 테스트 작성**
  - Character1의 35/50이 `0.7`, Character3의 0/80이 `0`으로 표시되는지 검증한다.
- [ ] **Step 2: RED 확인**
  - Unity가 열려 있으므로 batchmode는 사용하지 않고 Editor 테스트 실행 가능 여부를 확인한다. 불가능하면 컴파일과 기존 구현의 기본 fillAmount 1을 근거로 제한을 기록한다.
- [ ] **Step 3: 최소 구현**
  - `Render`에서 `HpBar/Fill` Image를 찾아 최대 HP 0 처리와 clamp를 거쳐 fillAmount를 설정한다.
- [ ] **Step 4: GREEN 검증**
  - Editor 어셈블리를 컴파일하고 가능한 테스트 실행 결과를 확인한다.

### Task 2: Battle 씬 HP 바 배치

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Produces: Character1~3 각각의 `HpBar/Fill` 계층

- [ ] **Step 1: 씬 계층 추가**
  - HP 텍스트 아래에 80x4 크기의 `HpBar`와 stretch된 `Fill`을 추가한다.
- [ ] **Step 2: UI 설정**
  - 배경은 어두운색, Fill은 붉은색 Filled/Horizontal/Left로 지정하고 Raycast Target을 끈다.
- [ ] **Step 3: 정적 검증**
  - 세 슬롯의 계층, 크기, fill 설정 및 presenter 경로가 일치하는지 확인한다.
