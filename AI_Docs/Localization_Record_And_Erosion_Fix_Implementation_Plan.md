# Localization Record And Erosion Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 침식 툴팁의 비동기 덮어쓰기를 제거하고 Localization Manager가 최신 게임데이터와 기록서 로컬리제이션을 함께 동기화하도록 한다.

**Architecture:** 동적 텍스트 소유권은 자동 바인딩 제외 컴포넌트로 명시한다. 게임데이터 변환은 재사용 가능한 동기 에디터 API로 만들고 Localization Manager 적용 파이프라인에서 호출한다.

**Tech Stack:** Unity 6, C#, Unity Localization, NUnit EditMode, PowerShell XLSX 변환기

**Spec:** `AI_Docs/Localization_Record_And_Erosion_Fix_Design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs/`, `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity batchmode 테스트는 실행하지 않는다.
- 기존 작업 트리의 관련 없는 변경은 보존한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 빠른 침식 선택 전환에서도 이전 비동기 번역이 최신 텍스트를 덮어쓰지 않아야 한다.
- 열린 Excel 파일도 공유 읽기로 변환할 수 있어야 한다.
- 변환 실패가 Localization Manager의 성공으로 보고되지 않아야 한다.
- Skill/Rune/Relic의 새 한국어 표시 필드는 안정적인 ID 키로 생성되어야 한다.
- 최신 `S_Unique_05`의 두 수치가 런타임 CSV와 기록서 설명에 모두 반영되어야 한다.

---

### Task 1: 침식 툴팁 텍스트 소유권

**Files:**
- Modify: `Assets/Project/Scripts/ErosionDifficultyCatalogUI.cs`
- Test: `Assets/Tests/EditMode~/ErosionTooltipLocalizationOwnershipTests.cs`

**Interfaces:**
- Consumes: `LocalizationAutoBindingIgnore`, 기존 로컬리제이션 컴포넌트
- Produces: 카탈로그 UI 단독 텍스트 쓰기 권한

- [ ] 실패 테스트에서 보호 함수 호출 후 자동 바인딩 제외 표식과 비활성 상태를 검증한다.
- [ ] 테스트가 현재 구현에서 실패하는 것을 확인한다.
- [ ] `EnsureDynamicTooltipTextOwnership`을 최소 수정한다.
- [ ] 대상 테스트와 관련 회귀 테스트를 통과시킨다.

### Task 2: Localization Manager 게임데이터 동기화

**Files:**
- Modify: `Assets/Editor/ExcelToBytesConverter.cs`
- Modify: `Assets/Editor/LocalizationManagerWindow.cs`
- Test: `Assets/Tests/EditMode~/LocalizationGameDataApplyTests.cs`

**Interfaces:**
- Produces: `ExcelToBytesConverter.ConvertOrThrow()`
- Consumes: Localization Manager `ApplySafe()`

- [ ] 임시 XLSX 변환 결과와 실패 전파를 검증하는 실패 테스트를 작성한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 기존 메뉴와 적용 파이프라인이 공유하는 동기 변환 API를 구현한다.
- [ ] 대상 테스트와 에디터 컴파일을 통과시킨다.

### Task 3: 기록서 키와 최신 수치 회귀 검증

**Files:**
- Modify: `Assets/Tests/EditMode~/LocalizationComprehensiveCoverageTests.cs`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`

**Interfaces:**
- Consumes: `GameData.xlsx`, `Localization.xlsx`, 변환된 Runtime CSV
- Produces: 기록서 표시 데이터 커버리지 보장

- [ ] Skill/Rune/Relic 표시 키 및 `S_Unique_05` 다중 수치 검증을 추가한다.
- [ ] 오래된 Runtime CSV로 실패하는 것을 확인한다.
- [ ] 승인된 변환기를 실행해 Runtime CSV를 갱신한다.
- [ ] 관련 테스트, 전체 EditMode 가능 범위, 런타임/에디터 어셈블리 빌드를 검증한다.
