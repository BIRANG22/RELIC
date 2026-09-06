# 타이틀 언어 드롭다운 수정 설계

## 문제

`LanguageDropdownUI`가 비활성 상태인 `Language` 탭 버튼에 부착되어 있다. 이 때문에 컴포넌트의 `Start()`가 실행되지 않고 실제 `LanguageContent` 드롭다운에 언어 변경 리스너가 등록되지 않는다.

## 설계

- `LanguageDropdownUI`의 코드와 직렬화된 드롭다운 참조는 유지한다.
- 컴포넌트만 항상 활성화되는 `Option` 루트로 이동한다.
- `LanguageContent`와 Localization Table, `TestText` 설정은 변경하지 않는다.
- 프리팹 EditMode 테스트로 `LanguageDropdownUI`와 `OptionPanelUI`가 같은 활성 오브젝트에 배치되고, 참조 드롭다운이 `LanguageContent` 하위인지 검증한다.

## 데이터 흐름

`Option` 활성화 → `LanguageDropdownUI.Start()` → Localization 초기화 대기 → 드롭다운 리스너 등록 → 사용자 선택 → `LocalizationSettings.SelectedLocale` 변경 → `Localize String Event`가 `TestText` 갱신

## 범위

타이틀 옵션의 언어 선택 초기화만 수정한다. 전투 상태 및 멀티플레이 동기화 구조에는 영향을 주지 않는다.
