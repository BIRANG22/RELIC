# Battle Reward Equip Panel Dynamic Localization Design

## 문제

배틀씬 `Equip_panel`의 Name, Rarity, Effect는 `BattleRewardEquipPanelUI`가 보상 데이터로 직접 갱신한다. 그러나 같은 TMP 오브젝트에 정적 `LocalizedTMPText`가 연결되어 언어 초기화 또는 언어 변경 때 Inspector 기본 문구로 다시 덮어쓸 수 있다.

## 결정

세 텍스트를 동적 출력 대상으로 명시한다. 배틀씬에서 정적 `LocalizedTMPText`를 제거하고 `BattleRewardEquipPanelUI` 초기화 시에도 `LocalizationIgnore`를 부착하고 정적 로컬라이저를 비활성화한다. 데이터 자체는 기존 `GameDataLocalization` 및 스킬 희귀도 표시 함수를 통해 현 언어에 맞춰 다시 계산한다.

## 검증

EditMode 테스트에서 세 TMP 오브젝트가 패널 초기화 후 `LocalizationIgnore`를 가지고 정적 `LocalizedTMPText`가 비활성화됐는지 검증한다.
