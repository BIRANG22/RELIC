# 언어별 폰트 역할 Database 구현 계획

> **실행 지침:** 현재 작업 공간에서 TDD로 순차 구현하며 커밋은 수행하지 않는다.

**목표:** Locale과 문자열 역할 ID로 TMP Font Asset을 선택하고, 언어 변경 시 대상 TMP에 안전하게 적용하는 데이터 기반 폰트 시스템을 구축한다.

**구조:** `LocaleFontDatabase`는 직렬화 데이터와 순수 조회/검증을 제공하고, Provider는 Resources 기본 DB를 읽기 전용으로 제공한다. `LocalizedTMPFont`가 각 TMP의 원본 폰트를 보존하면서 Locale 이벤트를 소비하고, Editor 도구가 역할 선택·검증·선택 범위 마이그레이션을 지원한다.

**기술:** Unity 6, C#, Unity Localization, TextMeshPro, ScriptableObject, CustomEditor/PropertyDrawer, NUnit EditMode

**설계:** `AI_Docs/2026-10-14-locale-font-role-database-design.md`

## 전역 제약

- 역할은 enum이 아닌 문자열 ID를 사용한다.
- 정확 Locale → 부모 Locale → 언어 defaultFont → 원본 폰트 → TMP 기본 폰트 순서를 유지한다.
- 문자열 Localization과 폰트 적용 책임을 결합하지 않는다.
- 전역 mutable static 폰트 저장소를 만들지 않는다.
- 프로젝트 전체 씬/프리팹을 일괄 수정하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.

## 검토 중점

- `zh-Hans`를 잘못 부모 코드로 축약하지 않는 정확 조회
- `en-US`의 `en` 부모 폴백과 정확 프로필 default 우선순위
- 원본 폰트 캡처 후 Locale 누락 시 복원
- 비활성화 중 이벤트 미수신 및 재활성화 시 최신 Locale 반영
- 기존 `OptionTMPFontEnforcer`와 동일 TMP 소유권 충돌 방지

### Task 1: Database 조회와 검증

**파일:**
- 신규: `Assets/Project/Scripts/Core/Localization/LocaleFontDatabase.cs`
- 신규 테스트: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

**인터페이스:**
- `TryResolve(string localeCode, string roleId, out TMP_FontAsset)`
- `GetRoleIds()`
- `Validate()`가 빈 값, 중복 Locale/역할, 누락 폰트 진단을 반환한다.

- [ ] 정확 조회, 부모 폴백, defaultFont, 조회 실패, 중복 검증 테스트를 작성하고 실패를 확인한다.
- [ ] 직렬화 프로필과 순수 조회/검증을 최소 구현한다.
- [ ] 테스트와 런타임 컴파일을 확인한다.

### Task 2: Provider와 LocalizedTMPFont

**파일:**
- 신규: `Assets/Project/Scripts/Core/Localization/LocaleFontDatabaseProvider.cs`
- 신규: `Assets/Project/Scripts/Core/Localization/LocalizedTMPFont.cs`
- 테스트: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

**인터페이스:**
- Provider는 `Resources/Localization/LocaleFontDatabase`를 읽기 전용으로 반환한다.
- `LocalizedTMPFont.Configure(LocaleFontDatabase, string, TMP_Text)`
- `RefreshForLocale(Locale)`은 DB 조회 성공 시 폰트만 교체하고 실패 시 최초 원본 폰트를 복원한다.

- [ ] 적용, 원본 복원, 재활성화, 연속 Locale 변경 테스트를 먼저 작성해 실패를 확인한다.
- [ ] 이벤트 구독과 원본 보존을 포함한 컴포넌트를 구현한다.
- [ ] Localization 전역 갱신기가 비활성 대상을 포함해 폰트도 갱신하도록 연결한다.
- [ ] 테스트와 런타임 컴파일을 확인한다.

### Task 3: Editor 지원과 공용 Database Asset

**파일:**
- 신규: `Assets/Editor/LocaleFontDatabaseEditor.cs`
- 신규: `Assets/Editor/LocaleFontRoleDrawer.cs`
- 신규: `Assets/Editor/LocalizedTMPFontMigrationTool.cs`
- 신규: `Assets/Resources/Localization/LocaleFontDatabase.asset`
- 신규 테스트: `Assets/Tests/EditMode~/LocaleFontEditorTests.cs`

**인터페이스:**
- Inspector는 DB 진단과 언어별 역할 누락을 표시한다.
- 역할 Drawer는 연결된 DB의 역할 목록을 제공한다.
- 마이그레이션 도구는 선택 계층/선택 프리팹만 기본 `body` 역할로 처리하고 폰트 소유권 충돌을 보고한다.

- [ ] DB Asset, Editor 클래스, 선택 범위 제한을 검증하는 실패 테스트를 작성한다.
- [ ] Editor와 공용 Asset을 구현한다.
- [ ] 공용 Asset에 `ko`, `en`, `es`, `ja`, `zh-Hans`와 `body/title/number`를 등록한다.
- [ ] 실제 폰트는 현재 프로젝트 에셋을 사용하되 미확정 라틴 폰트는 기존 DungGeunMo를 안전 기본값으로 둔다.
- [ ] Editor 컴파일과 정적 검증을 확인한다.

### Task 4: Option UI의 폰트 소유권 마이그레이션

**파일:**
- 수정: `Assets/Project/Scripts/OptionTMPFontEnforcer.cs`
- 수정: Option UI가 포함된 기존 프리팹 또는 씬
- 테스트: `Assets/Tests/EditMode~/LocaleFontEditorTests.cs`

**인터페이스:**
- `OptionTMPFontEnforcer`는 더 이상 LateUpdate에서 폰트를 직접 덮어쓰지 않는다.
- Option UI의 TMP는 `LocalizedTMPFont`를 단일 폰트 소유자로 사용한다.
- Dropdown Template에 붙은 컴포넌트가 런타임 복제본에 상속된다.

- [ ] 반복 폰트 강제 코드 제거와 소유권 단일화를 검증하는 실패 테스트를 작성한다.
- [ ] Option UI 적용 대상을 선택 범위로 마이그레이션한다.
- [ ] 직접 폰트 지정 코드 감사 결과를 문서에 기록한다.
- [ ] 관련 테스트와 Runtime/Editor 컴파일을 확인한다.

