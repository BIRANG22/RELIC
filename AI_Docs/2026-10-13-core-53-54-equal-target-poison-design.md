# S_Core_53/54 대상 중독 비례 부여 설계

## 목표

`S_Core_53`, `S_Core_54`가 대상이 현재 보유한 `E_Poison` 수치와 같은 양의 `E_Poison`을 대상에게 추가로 부여하도록 변경한다.

## 설계

- 신규 효과 ID는 `E_PoisonEqualTargetPoison`으로 정의한다.
- 효과 실행 시 대상의 현재 `E_Poison` 스택을 조회하고, 음수가 아닌 동일 수치만큼 `E_Poison`을 부여한다.
- 효과 수치는 대상 상태에서 결정하므로 `ValueRate`와 `CountRate`는 계산에 사용하지 않는다.
- 기존 `E_StrikeByTargetPoison`과 `E_PoisonByTargetPoison`은 다른 데이터와의 호환을 위해 유지한다.
- `S_Core_53`, `S_Core_54`는 신규 효과 ID를 사용하고 스킬 타입을 `Debuff`로 변경한다.
- 마스터 원본인 `GameData.xlsx`와 런타임 산출물인 `GameDataRuntime.csv`를 동일하게 맞춘다.

## 멀티플레이 경계

효과 계산은 대상의 전투 상태 ID/스택 데이터만 읽고 기존 `BattleEffectUtility` 상태 변경 경로를 사용한다. UI, VFX, 사운드에는 계산 책임을 추가하지 않는다.

## 검증

- 음수, 0, 양수 중독 수치의 계산 규칙을 EditMode 테스트로 검증한다.
- 신규 효과 ID가 `BattleEffectRegistry`에 등록되는지 검증한다.
- C# 프로젝트 컴파일과 데이터 원본/런타임 CSV 일치 여부를 검증한다.
