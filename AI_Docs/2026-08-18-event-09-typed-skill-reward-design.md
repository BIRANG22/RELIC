# Event_09 타입별 기억 보상 설계

## 문제

Event_09는 `ChoiceType=SelectReward`, `ResultType=OfferChoice`로 구성되어 있지만 실행 서비스가 이 타입을 실제 보상 큐로 처리하지 않는다. 또한 `CanSelect`가 `OfferChoice`와 `SelectReward`를 무조건 선택 불가로 막아 선택지가 비활성화된다.

## 권장 설계

- `OfferChoice`가 기억 보상이고 `ResultTarget`에 공격, 버프, 디버프 중 하나가 들어 있으면 지원되는 선택지로 본다.
- `ResultTarget`은 각각 `SkillType.Attack`, `SkillType.Buff`, `SkillType.Debuff`로 매핑한다.
- `ResultValue`에서 숫자를 읽어 보상 개수를 정하고, Event_09는 기본값 2개를 사용한다.
- 후보는 기존 랜덤 기억 보상과 같은 기준을 따른다.
  - `Category.Core`
  - 기본 변형 기억
  - 인벤토리, 장착 중 기억, 대기 중 보상, 강화 페어 제외
  - 선택한 `SkillType`과 일치
- 뽑힌 기억은 기존 `BattleRewardPanelUI` 전리품 패널에 `Skill` 보상으로 큐잉한다.

## 멀티플레이 경계

이벤트 선택 실행은 `SkillType`과 개수만 결과 콜백으로 넘긴다. 실제 런타임 보상 큐잉은 컨트롤러에서 ID 기반 `BattleRewardData`로 처리하며, UI 패널은 보상 선택/수령만 담당한다.
