# Localization Manager 구현 계획

**목표:** 기존 Localization 파이프라인을 보존하면서 프로젝트 텍스트의 검사·안전 연결·Excel 병합을 제공한다.

**구조:** Runtime 컴포넌트는 `GameLocalization`을 통해 표시만 갱신한다. EditorWindow와 scanner/apply pipeline은 에셋·Excel·코드를 검사하고 확실한 TMP 대상만 변경한다.

## 제약

- 문서는 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- 전투 상태/랜덤/네트워크 경계는 변경하지 않는다.
- Excel의 기존 번역은 덮어쓰거나 삭제하지 않는다.

### 작업 1: 런타임 TMP 바인딩

**파일:** `Assets/Project/Scripts/Core/Localization/LocalizedTMPText.cs`, `Assets/Tests/EditMode~/LocalizedTMPTextTests.cs`

1. Key와 Korean fallback으로 현재 언어를 적용하는 실패 테스트를 작성하고 실패를 확인한다.
2. Locale 변경 재적용과 빈 Key fallback 테스트를 추가한다.
3. `GameLocalization.Get` 기반의 최소 컴포넌트와 개발용 누락 registry를 구현한다.
4. EditMode 테스트를 실행한다.

### 작업 2: Excel 병합과 검사 모델

**파일:** `Assets/Editor/LocalizationWorkbookWriter.cs`, `Assets/Editor/LocalizationProjectScanner.cs`, `Assets/Tests/EditMode~/LocalizationProjectScannerTests.cs`

1. Korean 원문 중복의 기존 키 재사용, 신규 stable key, 동적 숫자 제외 테스트를 작성하고 실패를 확인한다.
2. 파일/행/사용처/진단을 표현하는 순수 검사 모델을 구현한다.
3. 신규 행만 Open XML workbook에 병합하고 백업하는 writer를 구현한다.
4. 테스트를 실행한다.

### 작업 3: Manager와 안전 Apply

**파일:** `Assets/Editor/LocalizationManagerWindow.cs`, `Assets/Editor/StaticLocalizationMigration.cs`, `Assets/Tests/EditMode~/LocalizationManagerApplyTests.cs`

1. LocalizeStringEvent 중복 없이 연결하는 테스트를 확장하고 실패를 확인한다.
2. 씬/프리팹 safe apply, 기존 키 유지, Ignore 판정을 구현한다.
3. Menu EditorWindow, 필터, 요약, Scan/선택 Apply/전체 안전 적용 UI를 구현한다.
4. Editor 프로젝트를 컴파일하고 EditMode 테스트를 실행한다.
