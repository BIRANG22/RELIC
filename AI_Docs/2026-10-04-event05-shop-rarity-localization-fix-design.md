# Event_05 후속 선택지 및 배틀상점 레어도 현지화 수정 설계

## 문제

1. `Event_05_A`와 `Event_05_B`에 표시되는 3번 선택지는 `Event_05`의 선택지를 복제하지만, 복제 과정에서 `EventId`가 현재 단계 ID로 바뀐다. UI는 이 ID로 번역 키를 만들기 때문에 존재하지 않는 `data.event.event_05_a.choice_3_*` 또는 `data.event.event_05_b.choice_3_*`를 조회한다.
2. 배틀상점 `GoodsIconItem`은 기억과 유물의 레어도 및 종류를 한국어 문자열로 직접 조합한다. 따라서 영어 등 다른 로케일에서도 한국어가 남는다.

## 권장 설계

### Event_05 후속 3번 선택지

- `GameDataLocalization.EventChoiceKey`가 `Event_05_A` 또는 `Event_05_B`의 3번 선택지를 번역할 때 원본인 `Event_05`의 3번 선택지 키를 사용한다.
- 선택지 실행에 필요한 현재 단계 `EventId`는 유지한다. 전투 흐름과 보상 처리는 변경하지 않는다.
- 번역 행을 복제하지 않고 이미 존재하는 `data.event.event_05.choice_3_name`과 `data.event.event_05.choice_3_description`을 재사용한다.

### 배틀상점 레어도

- 기억 레어도는 `SkillRarityUtility.GetDisplayName` 결과를 그대로 표시한다. 이 값은 이미 종류까지 포함한 로케일별 문자열이다.
- 유물 레어도는 `RelicRarityUtility.GetDisplayName`과 `common.relic`을 조합한다.
- 하드코딩된 한국어 레어도/종류 문자열을 제거한다.
- 기존 `LocaleTableReady` 재바인딩 흐름은 유지한다.

## 검증

- `Event_05_A`와 `Event_05_B`의 3번 선택지 키가 `Event_05`의 정식 키를 가리키는 회귀 테스트를 추가한다.
- 배틀상점 기억·유물 레어도 조합이 현지화 유틸리티를 사용하는지 회귀 테스트로 고정한다.
- Unity 런타임 및 에디터 어셈블리를 컴파일한다.
- Unity 에디터는 열려 있다고 가정하며 batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

UI 번역 키 선택과 표시 문자열만 변경한다. Command, 전투 상태 변경, Result/Event, 랜덤 및 네트워크 경계에는 영향이 없다.
