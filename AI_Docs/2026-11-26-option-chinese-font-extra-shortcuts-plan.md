# Option Chinese Font and Extra Shortcuts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 중국어 옵션 문구에 올바른 본문 폰트를 적용하고 Tab, 1~5, Space 전투 단축키를 설정 가능하게 한다.

**Architecture:** 기존 `BattleKeyboardShortcutBindings`를 확장하고 각 전투 입력 소비자가 설정값을 조회한다. 옵션 동적 번역 문구는 숫자 폰트 추론을 우회해 로케일 본문 폰트를 직접 적용한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity Localization, PlayerPrefs

**Spec:** `AI_Docs/2026-11-26-option-chinese-font-extra-shortcuts-design.md`

## Global Constraints

- 문서와 테스트 위치는 `AGENTS.md`를 따른다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 하지 않는다.
- 커밋, 브랜치, worktree, Push, PR을 수행하지 않는다.
- `B` 키는 추가하지 않는다.

## Review Focus

- 중국어 옵션 동적 문구가 body 폰트를 받는지 정적 경로 확인.
- 1~3 변경이 캐릭터 선택과 타임라인 선택 양쪽에 적용되는지 확인.
- 튜토리얼 허용 키가 변경된 키를 비교하는지 확인.
- 타임라인 KeyText가 변경 이벤트 후 즉시 갱신되는지 확인.
- 중복 키의 모든 항목이 빨간색이며 모든 소비자가 입력을 평가하는지 확인.

---

### Task 1: 바인딩 모델과 테스트 확장

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleKeyboardShortcutBindingsTests.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

- [ ] 기본값 `Tab`, `Alpha1~5`, `Space`와 중복 판정 테스트를 먼저 추가한다.
- [ ] `BattleShortcutAction`과 저장/표시 프로퍼티를 최소 확장한다.

### Task 2: 옵션 중국어 폰트와 행 표시

**Files:**
- Modify: `Assets/Project/Scripts/UI/Title/OptionPanelUI.cs`

- [ ] 번역 문구에 body 폰트를 명시 적용하는 헬퍼를 추가한다.
- [ ] 추가 단축키 행과 다국어 동작명을 기존 조작 UI에 노출한다.

### Task 3: 전투 입력과 표기 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/ReserveTurnSlotUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- Modify: `Assets/Project/Scripts/TutorialFocusGuideRunner.cs`

- [ ] 고정 키 입력을 바인딩 조회로 교체한다.
- [ ] 슬롯 표기와 튜토리얼 허용 키를 설정값에 연결한다.
- [ ] 전체 C# 컴파일, diff 검사, 정적 리뷰를 수행한다.

