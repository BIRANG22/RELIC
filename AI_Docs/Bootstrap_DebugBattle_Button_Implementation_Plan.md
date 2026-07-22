# Bootstrap DebugBattle Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bootstrap의 기존 자동 Title 전환을 그대로 유지하면서 Title 우측 하단 버튼으로 기본 파티가 준비된 DebugBattle에 진입한다.

**Architecture:** 씬 전환 컴포넌트는 버튼 요청과 자동 전환의 경쟁만 제어한다. 디버그 파티 생성은 별도 정적 서비스가 CharacterDatabase의 기본 제공 캐릭터와 마스터 스탯을 읽어 CharacterRuntimeStore 및 PartyRuntimeStore를 ID 기반으로 채운다.

**Tech Stack:** Unity 6, C#, Unity UI, NUnit EditMode 테스트

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
- UI는 전투 핵심 상태를 직접 계산하지 않고 디버그 데이터 구성 서비스에 요청한다.
- 커밋과 PR은 사용자 별도 허락 전에는 만들지 않는다.

---

### Task 1: 기본 디버그 파티 구성 서비스

**Files:**
- Create: `Assets/Project/Scripts/Debug/DebugBattlePartySetup.cs`
- Create: `Assets/Tests/EditMode~/DebugBattlePartySetupTests.cs`

**Interfaces:**
- Consumes: `DataManager`, `CharacterDatabase`, `CharacterRuntimeStore`, `PartyRuntimeStore`, `CharacterStartingRelicUtility`
- Produces: `DebugBattlePartySetup.TryCreateDefaultParty(DataManager dataManager)`

- [ ] 기본 제공 캐릭터만 최대 3명 선택하고 각기 다른 그리드 0, 1, 2에 넣는 실패 테스트를 작성한다.
- [ ] Unity 에디터가 열려 있어 batchmode 실행은 생략하고 테스트 파일의 컴파일 구조를 정적 검토한다.
- [ ] 마스터 HP/코스트/스킬/시작 유물로 런타임 데이터를 생성하는 최소 구현을 작성한다.
- [ ] Assembly-CSharp 빌드로 런타임 코드 컴파일을 검증한다.

### Task 2: Title 버튼과 DebugBattle 진입 연결

**Files:**
- Create: `Assets/Project/Scripts/Debug/TitleDebugBattleLauncher.cs`
- Modify: `Assets/Project/Scenes/YDM/Title.unity`

**Interfaces:**
- Consumes: `DebugBattlePartySetup.TryCreateDefaultParty(DataManager dataManager)`
- Produces: `TitleDebugBattleLauncher.LoadDebugBattle()`

- [ ] 버튼 클릭 시 단 한 번만 DebugBattle을 로드하는 테스트 항목을 추가한다.
- [ ] Title 진입 컴포넌트에 DebugBattle 씬 이름과 버튼 핸들러를 추가한다.
- [ ] Title Canvas 우측 하단에 기본 흰색 `Image`, `Button`을 직렬화해 배치한다.
- [ ] 버튼의 OnClick을 `LoadDebugBattle()`에 연결한다.

### Task 3: 씬 등록 및 검증

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Consumes: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Produces: 이름 기반 `SceneManager.LoadScene("DebugBattle")` 가능 상태

- [ ] DebugBattle 씬을 Build Settings의 Battle 다음에 활성 상태로 추가한다.
- [ ] Assembly-CSharp와 Assembly-CSharp-Editor를 빌드해 컴파일 오류가 없는지 확인한다.
- [ ] Bootstrap 씬 YAML에서 버튼 위치, OnClick 대상, DebugBattle 등록을 정적으로 확인한다.
- [ ] Unity 에디터에서 Bootstrap 재생 후 자동 Title 전환과 DebugBattle 버튼 진입을 수동 확인할 항목을 보고한다.
