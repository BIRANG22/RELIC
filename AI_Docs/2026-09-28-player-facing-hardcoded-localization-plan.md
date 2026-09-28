# Player-facing Hardcoded Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어 표시 한국어 하드코딩을 키 기반으로 전환하고 지정 TMP 오브젝트의 로컬라이제이션 소유권을 바로잡는다.

**Architecture:** `Localization.xlsx`와 Unity StringTable을 원본으로 두고 런타임 코드는 키만 요청한다. 정적·ID 동적·문구 동적·제외 컴포넌트를 명시적으로 구분하며 스캐너 테스트로 직접 한국어 재도입을 막는다.

**Tech Stack:** Unity, C#, Unity Localization, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-09-28-player-facing-hardcoded-localization-design.md`

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 현재 작업 공간과 사용자 변경을 보존한다.
- 커밋, 브랜치, worktree, Push, PR은 수행하지 않는다.
- 로그·주석·데이터 판정 토큰은 번역 대상에서 제외한다.

## Review Focus

- 영어 셀이 비었을 때 한국어가 아닌 `Untranslated`가 표시되는가.
- 패널 갱신 후에도 코드가 한국어 원문을 다시 쓰지 않는가.
- 포맷 키의 자리표시자와 전달 인자가 일치하는가.
- 동적 TMP에 중복 로컬라이저가 붙지 않는가.
- 기존 데이터 판정용 한국어 토큰을 실수로 번역하지 않았는가.

---

### Task 1: 감사 규칙과 회귀 테스트

**Files:**
- Modify: `Assets/Editor/LocalizationProjectScanner.cs`
- Create: `Assets/Tests/EditMode~/PlayerFacingHardcodedLocalizationTests.cs`

**Interfaces:**
- Produces: 플레이어 표시용 한국어 리터럴과 직렬화 필드를 분류하는 스캐너 API

- [ ] 플레이어 표시 sink 및 직렬화 필드가 검출되는 실패 테스트를 작성한다.
- [ ] 테스트가 기존 누락 때문에 실패하는 것을 확인한다.
- [ ] 스캐너 분류를 구현한다.
- [ ] 테스트 통과를 확인한다.

### Task 2: 키 기반 경고 UI

**Files:**
- Modify: `Assets/Project/Scripts/UI/Lobby/SettingWarningUI.cs`
- Modify: 관련 로비·타이틀 호출 스크립트
- Modify: `Assets/Project/Scripts/Core/Localization/LocalizationKeys.cs`
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_*.asset`

**Interfaces:**
- Produces: `SettingWarningUI.ShowKey(string, params object[])`

- [ ] 키 기반 번역과 포맷 동작의 실패 테스트를 작성한다.
- [ ] 실패를 확인한다.
- [ ] 한국어 인스펙터 메시지와 원문 비교 로직을 제거하고 호출부를 키 기반으로 바꾼다.
- [ ] 엑셀 및 StringTable 키를 동기화한다.
- [ ] 테스트 통과를 확인한다.

### Task 3: 나머지 플레이어 표시 하드코딩 전환

**Files:**
- Modify: 스캐너가 확정한 런타임 표시 스크립트
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_*.asset`

**Interfaces:**
- Consumes: Task 1의 분류 규칙
- Produces: 직접 한국어 출력이 없는 런타임 UI 코드

- [ ] 현재 위반 목록을 실패 테스트 fixture로 고정한다.
- [ ] 각 표시 문자열을 의미 기반 키 호출로 전환한다.
- [ ] 포맷 문장을 완성 문장 키로 전환한다.
- [ ] 감사 테스트 통과를 확인한다.

### Task 4: 지정 TMP 오브젝트 소유권 수정

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: 필요 시 해당 presenter 스크립트

**Interfaces:**
- Produces: 요청된 네 경로의 명시적인 로컬라이제이션 컴포넌트 구성

- [ ] 네 경로의 목표 컴포넌트를 검증하는 실패 테스트를 작성한다.
- [ ] 씬 컴포넌트와 키를 수정한다.
- [ ] 중복 writer 및 잘못된 자동 등록을 제거한다.
- [ ] 테스트 통과를 확인한다.

### Task 5: 전체 검증

**Files:**
- Verify: 모든 변경 파일

- [ ] EditMode 테스트를 Unity 에디터에서 실행 가능한 상태로 컴파일한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 스캐너 잔여 위반, `git diff --check`, Unity Editor 로그를 확인한다.
- [ ] 검증하지 못한 플레이 모드 동작을 명확히 기록한다.
