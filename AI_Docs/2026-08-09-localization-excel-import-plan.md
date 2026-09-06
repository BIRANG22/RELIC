# Localization Excel Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Localization.xlsx`의 번역을 Unity `Text` String Table Collection으로 안전하게 Merge Import한다.

**Architecture:** Editor 전용 Open XML reader가 `Text` 시트를 CSV로 변환하고 Unity Localization 공식 CSV API가 컬렉션을 갱신한다. 워크북이 단일 원본이며 Unity에서 Excel로 내보내지는 않는다.

**Tech Stack:** Unity 6, Unity Localization 1.5.11, C# Editor API, Open XML ZIP/XML, NUnit EditMode, XLSX

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 현재 브랜치와 작업공간을 사용하고 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: XLSX reader 계약

**Files:**
- Create: `Assets/Editor/LocalizationXlsxReader.cs`
- Test: `Assets/Tests/EditMode~/LocalizationExcelImporterTests.cs`

**Interfaces:**
- Produces: `LocalizationXlsxReader.ReadSheet(string path, string sheetName)`
- Produces: `LocalizationXlsxReader.ToCsv(IReadOnlyList<IReadOnlyList<string>> rows)`
- Produces: `LocalizationXlsxReader.ValidateHeaders(IReadOnlyList<IReadOnlyList<string>> rows)`

- [ ] 테스트에서 실제 `Localization.xlsx`의 `Text` 시트와 CSV 특수문자 escaping을 요구한다.
- [ ] 구현 전 컴파일 실패를 확인한다.
- [ ] ZIP/XML reader와 CSV 변환을 최소 구현한다.
- [ ] Editor 어셈블리가 컴파일되는지 확인한다.

### Task 2: Unity Localization Merge Import

**Files:**
- Create: `Assets/Editor/LocalizationExcelImporter.cs`
- Create: `Assets/ExcelSource/Localization.xlsx`

**Interfaces:**
- Consumes: `LocalizationXlsxReader.ReadSheet`, `ToCsv`, `ValidateHeaders`
- Produces: Unity menu `Tools/Localization/Import Localization Excel`

- [ ] `Text` 컬렉션 탐색과 공식 `Csv.ImportInto` Merge 호출을 구현한다.
- [ ] 현재 다섯 Locale과 `ui_start`가 포함된 워크북을 생성한다.
- [ ] 워크북 값, 수식 오류, 렌더링을 검사한다.
- [ ] 전체 Editor 어셈블리를 컴파일하고 변경 범위를 검토한다.

### Task 3: Excel 빈 예약 행 정규화

**Files:**
- Modify: `Assets/Editor/LocalizationXlsxReader.cs`
- Test: `Assets/Tests/EditMode~/LocalizationExcelImporterTests.cs`

- [ ] 완전히 빈 행과 헤더보다 짧은 행을 포함한 실패 테스트를 작성한다.
- [ ] 현재 CSV 변환 결과가 실패하는지 확인한다.
- [ ] 빈 행은 제외하고 짧은 행은 헤더 너비까지 채우도록 최소 수정한다.
- [ ] 실제 사용자가 수정한 워크북과 Editor 어셈블리를 다시 검증한다.
