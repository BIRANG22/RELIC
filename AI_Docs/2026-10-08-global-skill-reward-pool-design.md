# 전역 스킬 보상 ID 제한 설계

## 목적

전투, 이벤트, 시작방 등 스킬 보상이 생성되는 모든 경로에서 기획자가 지정한 스킬 ID만 후보가 되도록 제한한다.

## 조사 결과

- 전투 종료 스킬 보상은 `BattleRewardResolver`가 후보를 만들고 `SkillRewardRoller`가 드롭 확률과 등급을 판정한다.
- 이벤트 스킬 보상은 `EventRoomController`의 무작위, 조건부 다중 제시, 즉시 지급 경로가 공통 후보 수집 함수를 사용한다.
- 시작방 스킬 보상은 `RelicChoiceAreaUI`와 `StartRoomSkillRewardSelectionUtility`가 타입별 후보를 만든다.
- 휴식방 상점은 `RestRoomShopPanel`이 스킬 후보를 `RestRoomShopService`에 전달하고, 저장 재개 시 재고를 복원한다.
- 전투·이벤트·시작방 선택은 `BattleRandom`을 사용한다. 휴식방 상점은 기존 `UnitySkillRewardRandom`을 사용하고 있어 이번 전역 보상 변경과 함께 `BattleSkillRewardRandom`으로 통일한다.

## 권장 구조

### 설정 데이터

`SkillRewardPoolDatabase` ScriptableObject에 다음 값을 둔다.

- `RestrictRewards`: 전역 제한 사용 여부
- `AllowedSkillIds`: 보상으로 허용할 스킬 ID 목록

에셋은 `Assets/DB`에 두고 Bootstrap 및 DebugBattle의 `DataManager`에 명시적으로 연결한다. 제한이 꺼져 있으면 기존 후보를 그대로 사용하여 기존 프로젝트 동작을 보존한다. 제한이 켜져 있으면 목록에 있는 ID만 허용하며, 빈 값과 중복 ID는 무시한다. 제한이 켜졌지만 목록이 비었거나 유효 후보가 없으면 스킬 보상을 생성하지 않는다.

전용 인스펙터는 `GameDataRuntime`의 코어 기본형 스킬 ID를 드롭다운으로 제공한다. 목록에 남은 미존재 ID, 중복 ID, 제한 활성 상태의 빈 목록은 인스펙터 경고로 표시한다.

### 공용 정책

`SkillRewardPoolPolicy`는 후보 목록과 설정을 받아 허용된 후보만 반환하는 순수 정책으로 만든다. 랜덤 선택이나 소유 상태 계산은 담당하지 않는다.

각 보상 경로는 다음 순서를 유지한다.

1. 기존 경로의 카테고리, 등급, 스킬 타입, 기본형, 파티 적합성 조건 적용
2. 보유·장착·대기 보상 ID 제외
3. 전역 허용 ID 필터 적용
4. 기존 `BattleRandom` 기반 선택

따라서 전역 목록은 기존 보상 규칙을 우회하지 않고 최종 후보 범위만 좁힌다.

## 예외 정책

- 알 수 없는 ID: 후보 DB에 존재하지 않으므로 자동 제외
- 빈 문자열: 자동 제외
- 중복 ID: 하나로 취급
- 허용 ID가 모두 보유 중이거나 현재 경로 조건에 맞지 않음: 해당 스킬 보상 생략
- 시작방의 필요한 제시 개수보다 후보가 적음: 기존 규칙대로 해당 선택 보상을 만들지 않음
- 휴식방 저장 재개 재고에 현재 허용되지 않은 스킬이 있음: 해당 스킬 상품을 복원하지 않음

## 멀티플레이 경계

이 기능은 보상 후보를 안정적인 `SkillId`로 필터링하는 데이터 정책이다. 새 전역 런타임 상태나 Scene Object 의존성을 만들지 않으며, 결과 랜덤은 기존 `BattleRandom` 흐름을 유지한다. 동기화 환경에서는 동일한 설정 에셋과 전투 seed를 사용해야 같은 결과가 나온다.
