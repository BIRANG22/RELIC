# Localization Runtime Ownership and Event Choice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비와 배틀 이벤트 UI의 로컬라이징 소유권을 바로잡고, 누락된 선택지 설명 및 플레이어 노출 한국어 하드코딩을 ID 기반으로 전환한다.

**Architecture:** 정적 라벨은 씬의 `LocalizedTMPText`, 동적 숫자와 게임데이터 텍스트는 런타임 프레젠터가 단독 소유한다. 이벤트 선택지 키는 EventId·ChoiceOrder·필드로 생성하고 엑셀과 Unity String Table을 같은 키 집합으로 유지한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity Localization, NUnit EditMode tests, XLSX/YAML localization assets

**Spec:** `AI_Docs/2026-10-26-localization-runtime-ownership-and-event-choice-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 실행은 하지 않는다.
- 현재 체크아웃과 기존 미커밋 변경을 보존한다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- `선택지 설명`과 기존 `선택지 내용` 별칭이 동일한 키를 만드는지 검증한다.
- 같은 EventId에서 선택 순서가 다른 설명이 충돌하지 않는지 검증한다.
- 숫자 TMP가 로케일 변경 후에도 미번역 문자열로 대체되지 않는지 소유권을 검증한다.
- Event_02 예외 경로가 한국어 원문을 다시 덮어쓰지 않는지 검증한다.
- 로컬라이징 템플릿의 자리표시자가 영어·일본어에서도 보존되는지 검증한다.

---

### Task 1: 회귀 테스트 추가

**Files:**
- Modify: `Assets/Tests/EditMode~/LocalizationDynamicSourceScannerTests.cs`
- Create: `Assets/Tests/EditMode~/LocalizationRuntimeOwnershipRegressionTests.cs`

**Interfaces:**
- Consumes: `LocalizationProjectScanner.BuildEventChoiceKey`, 씬 YAML, 로컬라이징 워크북
- Produces: 키 생성·씬 소유권·번역 자산 일치 계약

- [ ] `선택지 설명`이 `data.event.event_02.choice_1_description`으로 변환되는 실패 테스트를 작성한다.
- [ ] 로비 라벨과 BlueDustiumHUD 및 이벤트 선택지 소유권의 실패 테스트를 작성한다.
- [ ] 현재 코드/씬에서 테스트가 기대한 이유로 실패함을 확인한다.

### Task 2: 스캐너 및 이벤트 표시 경로 수정

**Files:**
- Modify: `Assets/Editor/LocalizationProjectScanner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceSlotUI.cs`

**Interfaces:**
- Consumes: Event 시트 헤더, `GameDataLocalization.EventChoiceDescription`
- Produces: 선택 순서 기반 설명 키와 단일 런타임 TMP 소유권

- [ ] `선택지 설명` 별칭을 `choice_description`으로 정규화한다.
- [ ] Event_02의 한국어 원문 재대입 예외를 제거한다.
- [ ] 관련 테스트가 통과하는지 확인한다.

### Task 3: 로비 씬 로컬라이징 소유권 수정

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Project/Scripts/UI/LobbyBlueDustiumHudUI.cs`

**Interfaces:**
- Consumes: `ui.lobby.panel.erosion`, `ui.lobby.panel.resonance`, `LocalizedTMPText.ProtectDynamicOwnership`
- Produces: 로컬라이즈되는 정적 라벨과 번역 대상에서 제외된 숫자 값

- [ ] 침식·공명 TMP에 기존 키의 정적 로컬라이저를 연결한다.
- [ ] BlueDustiumHUD 숫자 TMP의 잘못된 로컬라이저를 제거하고 `LocalizationIgnore` 및 런타임 보호를 적용한다.
- [ ] 씬 소유권 회귀 테스트가 통과하는지 확인한다.

### Task 4: 이벤트 선택지 번역 데이터 동기화

**Files:**
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_ko.asset`
- Modify: `Assets/Language/Text_en.asset`
- Modify: `Assets/Language/Text_ja.asset`
- Modify if required: other locale table assets

**Interfaces:**
- Consumes: `GameData.xlsx` Event 시트의 EventId·ChoiceOrder·ChoiceDesc
- Produces: 모든 선택지 설명 키와 locale별 값

- [ ] 누락된 `choice_{order}_description` 행을 중복 없이 추가한다.
- [ ] 한국어 원문과 영어·일본어 번역 및 자리표시자를 검증한다.
- [ ] Unity Shared/locale String Table을 같은 ID와 값으로 동기화한다.
- [ ] 워크북·String Table 일치 검증을 통과시킨다.

### Task 5: 플레이어 노출 한국어 하드코딩 이관

**Files:**
- Modify: 플레이어 UI 출력이 확인된 런타임 C# 파일
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: Unity String Table assets
- Modify: `Assets/Tests/EditMode~/RuntimeKoreanHardcodingTests.cs`

**Interfaces:**
- Consumes: 플레이어 노출 문자열 및 가변 숫자
- Produces: `GameLocalization.Get/Format` 기반 표시 문자열

- [ ] 디버그·로그·파싱 토큰을 제외한 직접 출력 리터럴을 테스트로 고정한다.
- [ ] 문구별 안정적인 ID와 자리표시자 템플릿을 추가한다.
- [ ] 런타임 표시 코드를 ID 기반 호출로 교체한다.
- [ ] 하드코딩 회귀 테스트를 통과시킨다.

### Task 6: 전체 검증

**Files:**
- Verify only: 변경된 코드, 씬, 엑셀, String Table

**Interfaces:**
- Consumes: Task 1~5 결과
- Produces: 컴파일 및 정적 검증 증거

- [ ] EditMode 테스트 코드를 컴파일한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 엑셀 키 중복·필수 번역·자리표시자 및 String Table 동기화를 검사한다.
- [ ] 변경된 씬 YAML의 참조 무결성을 검사한다.
- [ ] Unity 에디터 화면 및 실제 로케일 전환은 미검증 항목으로 분리한다.
