# 타이틀 언어 드롭다운 수정 구현 계획

**목표:** 타이틀 옵션에서 언어를 선택하면 Locale과 `TestText`가 갱신되도록 한다.

**구조:** 기존 `LanguageDropdownUI`를 `Option` 루트로 이동하여 비활성 탭 버튼 생명주기 의존성을 제거한다. 코드 로직과 테이블 데이터는 유지한다.

**기술:** Unity 6, Unity Localization 1.5.11, TextMesh Pro, NUnit EditMode 테스트

## 작업 1: 프리팹 배치 회귀 테스트

- [ ] `Assets/Tests/EditMode~/OptionPanelUITests.cs`에 `LanguageDropdownUI`가 `OptionPanelUI`와 같은 오브젝트에 있고 실제 언어 드롭다운을 참조하는지 검사하는 테스트를 추가한다.
- [ ] 현재 프리팹에서 테스트가 실패하는지 확인한다.

## 작업 2: 컴포넌트 이동

- [ ] `Assets/Project/PrefabsR/Option.prefab`에서 기존 `LanguageDropdownUI` 컴포넌트를 `Option` 루트로 이동한다.
- [ ] 직렬화된 `languageDropdown` 참조와 설정값을 유지한다.
- [ ] EditMode 테스트와 C# 프로젝트 빌드를 검증한다.

## 제약

- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.
- 전투 핵심 로직은 변경하지 않는다.
