# 스크립트 검사 및 Record 기억 정보 동적 출력 정리

## 문제

- `Localization Manager`의 스크립트 검사는 한국어 리터럴을 자동 키 추가 후보로 취급한다. 실제 키를 소비하지 않는 리터럴도 추가됐다가 미사용 키 정리에서 삭제되어, 다음 검사에서 다시 `New`로 표시된다.
- `Record/Info/Memory/method`, `consumption`, `Point`는 `RecordPanelUI`가 스킬 데이터로 조합해 쓰는 동적 출력인데 정적 `LocalizedTMPText`가 함께 붙어 있어 값이 덮어써진다.

## 설계

1. 스크립트의 한국어 리터럴은 자동 추가하지 않는다. 스크립트 검사는 기존 키 사용 여부 확인에만 사용하고, 원문 리터럴은 항상 검토 대상(`Review`)으로 표시한다.
2. Record의 세 동적 TMP는 `StaticLocalizationMigration.ConfigureDynamicText`로 정리한다. 이 작업은 정적 `LocalizedTMPText`와 이전 `LocalizeStringEvent`를 제거하고 `LocalizationIgnore`를 추가한다.
3. 출력 문자열은 기존 `RecordPanelUI.RefreshMemoryInfo`가 소유한다. 해당 코드는 `ui.record.method`, `ui.record.consumption`, `ui.record.effect` 템플릿을 `GameLocalization.Format`으로 포맷한다.
4. 워크북의 세 템플릿 키는 각각 `방식 : {0}`, `소모 : {0}`, `효과 : {0}`를 원문으로 유지한다. 실제 소비처가 없는 `ui.record.point`는 전체검사·적용의 미사용 키 정리 대상이다.

## 검증

- 스크립트 리터럴 자동 추가 금지 단위 테스트
- Record 메모리 동적 TMP 소유권 정리 단위 테스트
- Editor 어셈블리 빌드
