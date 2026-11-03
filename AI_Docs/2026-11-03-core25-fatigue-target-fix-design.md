# S_Core_25 쇠약 대상 수정 설계

## 의도

`S_Core_25`가 범위 내 적에게 `E_Fatigue` 3을 부여하고 시전자 자신에게는 부여하지 않도록 효과 대상 해석을 수정한다.

## 원인

`FatigueEffect`가 `AddStatusToDefaultTarget()`을 사용한다. 플레이어가 몬스터를 공격하는 컨텍스트에는 `PlayerCaster`와 `MonsterTarget`이 함께 존재하며, 범용 함수가 `PlayerCaster`를 `MonsterTarget`보다 먼저 선택해 시전자에게 쇠약을 부여한다.

## 목표

- 플레이어 대상 컨텍스트에서는 `PlayerTarget`에 쇠약을 부여한다.
- 몬스터 대상 컨텍스트에서는 `MonsterTarget`에 쇠약을 부여한다.
- 시전자 필드는 상태 효과 대상의 대체값으로 사용하지 않는다.
- `S_Core_25`의 데이터, 범위 계산, 수치 3은 변경하지 않는다.

## 접근

`FatigueEffect`를 `WeakenEffect`, `VulnerableEffect`와 같은 명시적 대상 분기 구조로 변경한다. 플레이어 시전자와 몬스터 대상이 동시에 존재하는 실제 공격 컨텍스트를 테스트해 시전자에게는 상태가 없고 몬스터에게만 3스택이 생기는지 검증한다.

## 멀티플레이 경계

명령과 범위 결과는 변경하지 않고, 효과 컨텍스트에 이미 포함된 명시적 Target 참조에만 상태 변경을 적용한다. 별도의 네트워크 상태나 랜덤은 추가하지 않는다.
