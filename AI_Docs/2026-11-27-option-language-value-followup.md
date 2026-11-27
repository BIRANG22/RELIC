# 옵션 재개방 시 한국어 번역으로 덮어쓰기 후속 수정

기존 5번 수정의 미해결 증상에 대한 후속 작업이며 기존 구현 승인 범위다.

## 확인한 원인

- OptionPanelUI는 SelectedLocale에서 English 등을 올바르게 작성한다.
- 이전 수정은 LocalizationIgnore 및 ProtectDynamicOwnership으로 정적 번역기만 차단했다.
- RuntimeTMPTextAutoLocalizer.TryAttach는 LocalizationIgnore 대상에 오히려 DynamicLocalizedTMPText를 붙인다.
- 한국어 선택 시 값 `한국어`가 원문으로 저장되면, 영어로 바꾼 후에는 비한국어 문구를 다시 추적하지 않아 이전 원문이 남는다.
- DynamicLocalizedTMPText.OnEnable이 남은 한국어 원문을 현재 언어로 번역해 옵션 재개방 시 Korean/韓国語 등으로 덮어쓴다.

## 수정

- 언어 값 TMP에 기존 LocalizationAutoBindingIgnore를 프리팹으로 배치해 자동 원문 매칭 자체를 제외한다.
- OptionPanelUI의 바인딩 시 동일 표식을 보장하고, 기존 정적/레거시 번역기 외 DynamicLocalizedTMPText도 비활성화한다. 이미 생성된 옵션 인스턴스도 복구한다.
- 전역 번역 동작이나 다른 UI의 LocalizationIgnore 의미는 변경하지 않는다.
- 언어 선택과 저장 로직은 기존대로 두고 표시 소유권만 수정한다.

## 회귀 검증

- OptionLanguageValueOwnershipTests: 한국어 원문이 기억된 실제 동적 writer와 정적/레거시 writer를 구성하고 옵션 재바인딩 후 5개 외국어 표시 및 직접 전역 갱신에도 문구가 보존되는지 검사한다.
- Unity 에디터 직접 제어/batchmode 금지에 따라 NUnit은 컴파일까지만 확인하고, 실제 재개방/언어 전환 화면 검증은 미검증으로 보고한다.

## 실행 결과

- 수정 전 프리팹: 언어 ValueText에 LocalizationAutoBindingIgnore가 없어 자동 동적 번역기 등록 경로가 열려 있음을 확인했다.
- 수정 후: 언어 ValueText에 자동 바인딩 제외 표식 연결, 컴포넌트 소유 GameObject, YAML 오브젝트 ID 중복 없음 확인.
- 전체 Assembly-CSharp 독립 Roslyn 컴파일: 종료 코드 0.
- 새 OptionLanguageValueOwnershipTests C# 컴파일: 종료 코드 0. Unity 실행이 필요한 5개 케이스는 미실행.
- 기존 독립 관리 NUnit 26개 통과. 이전에 확인한 RecordMonsterDiscoveryTests.Normalize_RemovesBlankAndCaseInsensitiveDuplicates 1개 실패는 동일하며 이번 수정과 무관하다.
- 변경 파일 diff --check 통과.
- 변경 파일: OptionPanelUI.cs, Option.prefab, OptionLanguageValueOwnershipTests.cs, 이 문서.
- 멀티플레이 영향 없음. 커밋/Push/PR 미수행.
