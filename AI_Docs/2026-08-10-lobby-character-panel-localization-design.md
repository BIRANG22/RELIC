# 로비 캐릭터 패널 번역 누락 수정 설계

## 배경

로비씬 캐릭터 패널에서 다음 텍스트가 런타임 언어 변경을 따르지 않는다.

- 스킬 정보 라벨 `Infotext_2`, `Infotext_3`
- 잠긴 `RuneSlotButton_2~5` 호버 시 `EffectText`
- `Info_Area > info > Text`에 표시되는 HP, Cost, Recovery 호버 툴팁

## 원인

- `Infotext_2`, `Infotext_3`에는 `LocalizeStringEvent`가 없어 고정 한국어 텍스트가 표시된다.
- `RuneSettingPanel.ShowRuneSlotInfo`는 잠긴 슬롯 안내 문구를 코드에서 직접 생성한다.
- `CharacterStatTooltipTarget`은 스탯 이름, 설명, 수치 라벨을 코드 하드코딩 문자열로 생성한다.

## 설계

- 기존 공용 키는 재사용한다.
  - `common.hp`
  - `common.cost`
  - `common.recovery`
  - `common.move_point`
  - `lobby.rune_slot_locked`
- 새로 필요한 UI 문구만 `Text` 테이블에 추가한다.
  - `lobby.skill_info.method_label`
  - `lobby.skill_info.cost_label`
  - `lobby.rune_slot_unlock_level`
  - `lobby.stat.hp.description`
  - `lobby.stat.cost.description`
  - `lobby.stat.recovery.description`
  - `lobby.stat.move.description`
  - `lobby.stat.base_value`
  - `lobby.stat.rune_bonus`
- `Infotext_2`, `Infotext_3`는 `LocalizeStringEvent`를 씬에 추가해 각각 새 라벨 키에 연결한다.
- `RuneSettingPanel.ShowRuneSlotInfo`는 잠긴 슬롯 문구를 `GameLocalization.Format/Get`으로 생성한다.
- `CharacterStatTooltipTarget`은 기본 문구를 `GameLocalization.Get/Format`으로 생성하되, 인스펙터의 `customName`, `customDescription` 오버라이드는 기존처럼 우선 적용한다.

## 검증

- EditMode 정적 테스트로 엑셀 `Text` 시트, Unity StringTable, 로비 씬 연결, 코드 키 사용을 확인한다.
- Unity 에디터는 열려 있다고 가정하므로 batchmode 테스트는 실행하지 않는다.
- MSBuild와 정적 검증 스크립트로 컴파일 및 데이터 정합성을 확인한다.
