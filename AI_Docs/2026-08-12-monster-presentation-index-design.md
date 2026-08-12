# 몬스터 프레젠테이션 인덱스 수정 설계

## 문제

`MonsterRuntimeData.GetPresentationActionIndexForSkill`이 공격과 비공격 행동을 각각 1부터 계산한다. 반면 `BattleUnitAnimator.monsterActionPresentations`는 하나의 연속 배열이므로 공격과 비공격 행동이 같은 슬롯 번호를 공유하면서 Arabella의 스킬 VFX가 앞 슬롯 VFX로 잘못 재생된다.

## 권장 설계

- 실제 이동 스킬은 기존처럼 프레젠테이션 배열에서 제외하고 인덱스 `0`을 반환한다.
- 이동을 제외한 모든 행동은 `TimelineNotation` 종류와 관계없이 몬스터의 보유 스킬 순서대로 `1, 2, 3...`의 단일 연속 인덱스를 사용한다.
- `BattleUnitAnimator`와 Arabella 프리팹의 현재 연속 슬롯 구성은 변경하지 않는다.
- 전투 상태나 결과 계산은 변경하지 않고, 결과를 표현하는 애니메이션/VFX 선택만 바로잡는다.

## 검증 기준

- `Move, Attack, Attack, Buff, Debuff` 순서의 몬스터에서 Move는 `0`, 나머지는 각각 `1, 2, 3, 4`를 반환한다.
- `E_Move` 효과로 판정되는 이동 행동도 인덱스에서 제외한다.
- 기존 컴파일과 관련 EditMode 테스트에 회귀가 없어야 한다.

