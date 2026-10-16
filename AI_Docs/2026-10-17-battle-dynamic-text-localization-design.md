# Battle Dynamic Text Localization Design

## 문제

영어 로케일에서도 `BattleEffectInfo`의 행동/효과 이름, 공유 `NextButton`의 진행·획득 문구, 이벤트의 레드 더스티움·유물 획득 결과가 한국어로 표시된다. 정적 TMP 바인딩은 존재하지만 런타임 프레젠터가 한국어 원문 또는 GameData 원문을 직접 다시 쓰며, 일부 동적 형식 키는 `Localization.xlsx`에 없다.

## 설계

- 동적 TMP의 표시 소유권은 기존 프레젠터에 유지한다. 정적 `LocalizedTMPText`가 임의의 동적 값을 덮어쓰게 하지 않는다.
- 고정 UI 문구는 `GameLocalization.Get`을 사용하고 숫자·이름이 포함된 문구는 `GameLocalization.FormatWithFallback`을 사용한다.
- 스킬, 몬스터 스킬, 효과, 룬, 유물 이름은 안정적인 데이터 ID를 사용하는 기존 `GameDataLocalization` API로 해석한다.
- 공유 NextButton은 화면별 프레젠터가 현재 용도에 맞는 번역 결과를 기록한다. 진행은 기존 `common.proceed`를 재사용하고 획득 개수는 전용 형식 키를 사용한다.
- 이벤트 결과용 레드 더스티움 및 유물 획득 형식 키와 배틀 효과 피드의 고정 알림 키를 `Localization.xlsx`에 5개 언어로 추가한다.
- 전체 검사기가 `text = $"..."`, `resultMessage = $"..."`, `return $"..."` 형태의 보간 동적 표시 문자열도 발견하도록 확장한다. 자동 코드 변환은 하지 않지만 신규 키 후보에서 조용히 누락되지 않게 한다.

## 검증

- EditMode 회귀 테스트로 스캐너의 보간 문자열 탐지와 엑셀의 키/번역 완전성을 확인한다.
- 관련 런타임 소스가 한국어 표시 리터럴을 직접 쓰지 않는지 회귀 검사한다.
- C# 프로젝트를 빌드해 컴파일을 확인한다.

## 멀티플레이 경계

UI 표시 문자열만 변경한다. Command, 전투 상태, 결과 이벤트, 랜덤 및 네트워크 동기화 데이터는 변경하지 않는다.

## 보상 슬롯 추가 원인 및 설계

`BattleRewardCanvas/BattleRewardPanelUI/Contant/RewardSlot(Clone)/Text`는 이벤트 결과 문구가 아니라 `BattleRewardSlotUI.Refresh()`가 최종 표시 문자열을 직접 소유한다. 이 메서드의 Remnant와 Relic 분기가 각각 `레드 더스티움`, `유물 획득`을 하드코딩했으므로 엑셀 전체 검사·적용이나 재시작으로는 바뀌지 않았다.

- 슬롯 전용 키 `battle.reward.red_dustium`, `battle.reward.relic_acquired`를 5개 언어로 추가한다.
- `BattleRewardSlotUI`는 위 키를 `GameLocalization.Get`으로 조회하고 기존 수량 줄바꿈 형식은 유지한다.
- 동적 슬롯 Text의 표시 소유권과 아이콘·클릭·보상 지급 로직은 변경하지 않는다.
