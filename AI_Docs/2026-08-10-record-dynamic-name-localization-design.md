# Record 동적 이름 Localization 설계

## 목표

`Record.prefab`의 `Info/Name` 텍스트가 정적 키 `common.name`에 묶이지 않고, 선택된 스킬, 룬, 유물, 완성 아이템의 현재 Locale 표시명을 보여주게 한다.

## 원인

- `Record/Info/Name`은 `RecordPanelUI.nameText`가 선택 항목 이름을 쓰는 동적 표시 영역이다.
- 같은 TMP 오브젝트에 `LocalizeStringEvent(Text/common.name)`가 붙어 있어 Locale 갱신 시 정적 문구 "이름"으로 덮일 수 있다.
- `RecordPanelUI`는 슬롯을 만들 때 `skill.Name`, `rune.Name`, `relic.Name`, `item.Name`을 직접 전달해 데이터 Localization API를 거치지 않는다.

## 권장 구조

- `Record/Info/Name`의 `LocalizeStringEvent`를 제거한다.
- 데이터 이름 변환은 `RecordPanelUI.cs` 하단의 `RecordDisplayNameResolver`로 분리한다.
- `RecordPanelUI`는 슬롯 생성 전에 다음 API로 표시명을 계산한다.
  - `RecordDisplayNameResolver.SkillName(SkillMasterData)`
  - `RecordDisplayNameResolver.RuneName(RuneData)`
  - `RecordDisplayNameResolver.RelicName(RelicData)`
  - `RecordDisplayNameResolver.ItemName(ItemData)`
- resolver는 `GameDataLocalization`을 우선 사용하고, 값이 비어 있으면 안정 ID를 fallback으로 사용한다.

## 검증

- EditMode 테스트로 `Record.prefab`의 `nameText` 대상에 `LocalizeStringEvent`가 남아 있지 않은지 확인한다.
- EditMode 테스트로 resolver가 원본 이름 대신 주입된 localized 값을 우선하는지 확인한다.
- MSBuild로 C# 컴파일을 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않는다.

## 멀티플레이 영향

표시 문자열만 바꾸며 전투 Command, State Change, Result/Event, 저장 데이터, 네트워크 스냅샷에는 영향을 주지 않는다.
