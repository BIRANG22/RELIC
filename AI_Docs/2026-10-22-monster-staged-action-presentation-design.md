# 몬스터 단계형 행동 연출 설계

## 목적

아라벨라의 도약 공격처럼 하나의 스킬이 `이륙 애니메이션/VFX → 그리드 이동 → 착지 애니메이션/VFX` 순서로 실행되는 연출을 지원한다.

## 설계

- `BattleUnitActionPresentation`의 기존 Prepare 단계를 이륙 단계에 사용하고 `prepareVfx`를 추가한다.
- 기존 `stateName`과 `vfx`는 Action(착지) 단계로 유지한다.
- `BattleUnitAnimator`는 Prepare와 Action을 각각 재생할 수 있는 API를 제공한다.
- 일반 몬스터 스킬의 기존 자동 `Prepare → Action` 흐름은 유지한다.
- `BattleActionRunner.ExecuteArabellaLeap`만 분리 API를 사용하여 Prepare 이후 실제 이동을 수행하고, 이동 완료 뒤 Action을 재생한다.
- 그리드 유효성, 점유, Transform, `GridIndex`, 피해와 넉백은 애니메이터가 변경하지 않는다.

## 실행 순서

1. 예약된 `MonsterReservedCommand`로 Prepare 연출을 선택한다.
2. 이륙 애니메이션과 이륙 VFX를 재생하고 `prepareDuration`만큼 기다린다.
3. `BattleActionRunner`가 목적지 점유 처리와 이동을 수행한다.
4. 착지 애니메이션과 착지 VFX를 재생한다.
5. 착지 범위 피해와 넉백 결과를 적용한다.

## 검증 기준

- Prepare 전용 호출은 Prepare VFX만 생성하고 설정된 대기 시간을 반환한다.
- Action 전용 호출은 Action VFX만 생성한다.
- 아라벨라 도약 실행 경로에서 Prepare와 Action 사이에 위치/그리드 상태 변경이 존재한다.
- 일반 몬스터 스킬의 기존 자동 연출 흐름에 회귀가 없다.

## 멀티플레이 경계

애니메이션과 VFX는 전투 결과를 계산하지 않는다. 안정적인 `RuntimeId`, `MonsterId`, `SkillId`, `GridIndex`가 들어 있는 Command를 기준으로 `BattleActionRunner`가 상태를 변경하고, `BattleUnitAnimator`는 해당 단계의 결과 연출만 재생한다.
