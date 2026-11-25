# 옵션 언어 표시 및 전투 단축키 설정 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 옵션 재오픈 언어 표시 오류를 해결하고 전투 단축키 9개를 조작 탭에서 변경하여 실제 입력과 전투 키 안내에 반영한다.

**Architecture:** 동작 ID 기반 `BattleKeyboardShortcutBindings`를 로컬 설정 저장소로 확장하고 변경 이벤트를 제공한다. 옵션 UI와 전투 키 표시 컴포넌트는 이 저장소만 읽으며, 프리팹/씬에는 명시적인 동작 ID 참조를 배치한다.

**Tech Stack:** Unity 6, C#, PlayerPrefs, TextMeshPro, Unity UI, NUnit EditMode

**Spec:** `AI_Docs/2026-11-26-option-language-shortcut-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 사용하지 않는다.
- UI는 전투 결과를 계산하지 않고 로컬 입력 매핑만 제공한다.
- 커밋, Push, PR, 브랜치/worktree 작업은 수행하지 않는다.

## Review Focus

- 같은 키가 여러 동작에 지정되면 모든 동작 조회가 그 키를 반환하고 모든 중복 행이 표시된다.
- 잘못 저장된 `KeyCode` 값은 해당 동작의 기본값으로 복구된다.
- `Escape` 입력은 재바인딩을 취소하고 Escape 자체를 저장하지 않는다.
- 비활성화 후 재활성화된 옵션과 전투 키 라벨이 현재 값을 다시 표시한다.
- 타임라인 숫자 1~5 단축키와 디버그 전용 키는 변경 대상에 포함되지 않는다.

---

### Task 1: 단축키 저장소와 회귀 테스트

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Create: `Assets/Tests/EditMode~/BattleKeyboardShortcutBindingsTests.cs`

**Interfaces:**
- Produces: `BattleShortcutAction`, `BattleKeyboardShortcutBindings.Get`, `Set`, `ResetAll`, `IsDuplicate`, `GetDisplayName`, `Changed`

- [ ] 기본값, 저장/로드, 중복 허용/판정, 전체 복구 테스트를 먼저 작성하고 실패를 확인한다.
- [ ] 상수 기반 바인딩을 PlayerPrefs 기반 동작 ID 저장소로 최소 변경한다.
- [ ] 테스트를 다시 실행해 통과를 확인한다.

### Task 2: 옵션 언어 표시 소유권 수정

**Files:**
- Modify: `Assets/Project/Scripts/UI/Title/OptionPanelUI.cs`
- Modify: `Assets/Project/PrefabsR/Option.prefab`
- Create: `Assets/Tests/EditMode~/OptionPanelLanguageOwnershipTests.cs`

**Interfaces:**
- Consumes: `LocalizationSettings.SelectedLocale`
- Produces: 정적 언어 제목과 동적 선택 언어명이 분리된 프리팹 참조

- [ ] 잘못된 언어값 참조와 재활성화 즉시 동기화를 검출하는 실패 테스트를 작성한다.
- [ ] 현재 언어명 전용 TMP 참조를 프리팹에 연결하고 `OnEnable`에서 즉시 갱신한다.
- [ ] 관련 테스트를 통과시킨다.

### Task 3: 옵션 조작 패널 및 재바인딩

**Files:**
- Create: `Assets/Project/Scripts/UI/Title/BattleShortcutOptionPanelUI.cs`
- Modify: `Assets/Project/PrefabsR/Option.prefab`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_ko.asset`
- Modify: `Assets/Language/Text_en.asset`
- Modify: `Assets/Language/Text_ja.asset`
- Modify: `Assets/Language/Text_zh-Hans.asset`
- Modify: `Assets/Language/Text_zh-Hant.asset`
- Modify: `Assets/Language/Text_es.asset`
- Create: `Assets/Tests/EditMode~/BattleShortcutOptionPanelTests.cs`

**Interfaces:**
- Consumes: Task 1의 바인딩 API
- Produces: 9개 키 행, 입력 대기/취소, 중복 빨간색 표시, 전체 기본값 복구

- [ ] 중복 색상 정책, Escape 취소, 프리팹의 9개 동작 ID와 `controlContent` 연결을 검증하는 실패 테스트를 작성한다.
- [ ] 옵션 패널 컴포넌트와 프리팹 조작 콘텐츠를 구현한다.
- [ ] 조작명/대기/중복/기본값 복구 문구를 6개 언어 테이블에 추가한다.
- [ ] 관련 테스트를 통과시킨다.

### Task 4: 전투 입력과 키 안내 연동

**Files:**
- Create: `Assets/Project/Scripts/UI/Battle/BattleShortcutKeyLabel.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Create: `Assets/Tests/EditMode~/BattleShortcutPresentationTests.cs`

**Interfaces:**
- Consumes: Task 1의 `Get(action)`, `GetDisplayName(action)`, `Changed`
- Produces: 현재 바인딩을 사용하는 전투 입력 및 Q/W/E/R/F/D/C/Z/X 안내 갱신

- [ ] 전투 입력 코드가 동적 API를 사용하고 9개 씬 라벨에 정확한 동작 ID가 연결되는 실패 테스트를 작성한다.
- [ ] 전투 입력 호출부와 튜토리얼 판정을 동적 속성으로 전환한다.
- [ ] 키 라벨 컴포넌트를 추가하고 두 전투 씬의 기존 KeyText에 명시적으로 연결한다.
- [ ] 관련 테스트를 통과시킨다.

### Task 5: 전체 검증

**Files:**
- Verify only

- [ ] 새 EditMode 테스트와 관련 기존 로컬라이즈/전투 회귀 테스트를 실행한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 컴파일한다.
- [ ] YAML 참조, `git diff --check`, 변경 범위를 검토한다.
- [ ] 에디터 화면 확인이 필요한 항목을 미검증으로 분리해 보고한다.
