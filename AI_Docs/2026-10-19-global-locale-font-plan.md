# Global Locale Font Application Implementation Plan

> **For agentic workers:** 현재 작업 공간에서 테스트 우선으로 직접 실행한다. 사용자 승인 없이 커밋, 브랜치 또는 worktree를 만들지 않는다.

**Goal:** 모든 `TMP_Text`가 현재 Locale의 DB 폰트를 자동으로 사용하도록 한다.

**Architecture:** 전역 런타임 적용기가 씬·Locale·TMP 변경 이벤트를 구독한다. 명시적인 `LocalizedTMPFont`는 기존 역할을 유지하고 나머지는 `body` 역할로 처리한다.
기존 스타일 폰트는 DB의 `sourceFontRoles`에서 역할을 복원한 뒤 Locale별 역할 폰트로 변환한다.

**Tech Stack:** Unity 6, C#, TextMesh Pro, Unity Localization, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-19-global-locale-font-design.md`

## Global Constraints

- 문서 파일은 `AI_Docs` 아래에만 둔다.
- 테스트는 `Assets/Tests/EditMode~` 아래에만 둔다.
- Unity batchmode 테스트를 실행하지 않는다.
- 기존 역할 폰트와 개별 DB 주입을 보존한다.
- 전투/멀티플레이 상태에는 관여하지 않는다.

## Review Focus

- Locale 초기화 전 호출 시 기존 폰트 유지
- DB 누락 시 기존 폰트 유지
- 명시적 `title`/`number` 역할 보존
- 이미 적용된 폰트의 불필요한 재대입 방지
- 런타임 생성 TMP의 변경 이벤트 적용

---

### Task 1: 전역 폰트 적용 규칙

**Files:**
- Create: `Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs`
- Modify: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

**Interfaces:**
- Consumes: `LocaleFontDatabase.TryResolve`, `LocalizedTMPFont.RefreshForLocale`
- Produces: `GlobalLocalizedTMPFontApplicator.ApplyToText(TMP_Text, LocaleFontDatabase, Locale)`

- [ ] 역할 미지정, 명시적 역할, 누락 데이터 테스트를 먼저 추가한다.
- [ ] 테스트가 새 API 부재로 실패하는 것을 확인한다.
- [ ] 단일 TMP 적용 규칙을 구현한다.
- [ ] 컴파일과 테스트 통과를 확인한다.

### Task 2: 런타임 전체 자동 갱신

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs`
- Modify: `Assets/Tests/EditMode~/LocaleFontEditorTests.cs`

**Interfaces:**
- Consumes: Task 1의 `ApplyToText`
- Produces: 씬 로드, Locale 변경, TMP 변경 이벤트와 렌더 전 신규 TMP 탐지 기반 자동 갱신

- [ ] 런타임 훅 존재 여부와 기존 전체 탐색 조건을 검증하는 테스트를 먼저 추가한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 이벤트 구독, 렌더 전 신규 TMP 탐지, Domain Reload 초기화와 비활성 포함 전체 갱신을 구현한다.
- [ ] 런타임/에디터 코드 컴파일과 관련 테스트를 검증한다.

### Task 3: 기존 스타일 폰트 호환

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/LocaleFontDatabase.cs`
- Modify: `Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs`
- Modify: `Assets/Resources/Localization/LocaleFontDatabase.asset`
- Modify: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

- [x] 프로젝트 전체의 MASTOD 사용 TMP를 조사한다.
- [x] 소스 폰트에서 역할을 찾는 DB 매핑과 조회 API를 추가한다.
- [x] 구형 MASTOD를 `number` 역할로 등록한다.
- [x] 구형 MASTOD는 동명의 다른 DB 에셋으로 교체하지 않고 원본을 유지한다.
- [x] 런타임 생성 TMP에도 매핑 역할이 적용되는 회귀 테스트를 추가한다.
- [x] 런타임/에디터 코드 컴파일을 확인한다.
