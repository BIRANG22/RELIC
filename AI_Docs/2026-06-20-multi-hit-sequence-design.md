# Multi-Hit Skill Sequence Design

## Goal
`Count`가 2 이상인 공격 스킬은 데미지만 합산하거나 한 프레임에 몰아서 처리하지 않는다. 각 히트마다 `공격 애니메이션/VFX -> 피격 데미지/애니메이션/VFX -> 카메라 임팩트` 순서로 실행한다.

## Current Behavior
- `StrikeEffect`와 `PierceEffect`는 `BattleEffectContext.Count`만큼 내부 루프를 돌며 데미지를 적용한다.
- 루프가 동기식으로 한 번에 끝나기 때문에 체력은 여러 번 감소해도 공격/피격 연출과 카메라 임팩트는 반복 히트처럼 보이지 않는다.
- `BattleActionRunner`는 스킬 액션 애니메이션을 1회 재생한 뒤 효과 실행과 카메라 임팩트를 1회 처리한다.

## Required Behavior
예: `Value = 10`, `Count = 3`인 공격 효과.

```text
Attack 1
Hit damage 10 + hit/guard/dead presentation 1 + camera impact 1
Attack 2
Hit damage 10 + hit/guard/dead presentation 2 + camera impact 2
Attack 3
Hit damage 10 + hit/guard/dead presentation 3 + camera impact 3
```

- 각 히트마다 `Value`만큼 데미지를 적용한다.
- 각 히트마다 공격자 공격 애니메이션/VFX를 재생한다.
- 각 히트마다 대상 피격/가드/사망 애니메이션과 VFX를 재생한다.
- 각 피격마다 `BattleCameraController.PlayDamageImpact()`를 실행한다.
- 대상이 사망하면 남은 히트는 실행하지 않는다.
- 버프, 디버프, 상태이상처럼 `Count`가 지속 턴/스택 의미인 효과는 기존 의미를 유지한다.

## Approach
멀티히트 반복은 `BattleActionRunner`와 `MonsterSkillEffectService`의 실행 계층에서 담당한다. 데미지 효과는 한 번 호출될 때 한 히트만 처리하도록 조정한다.

## Data Flow
1. 플레이어 스킬 실행 시 `SkillEffectEntry`에서 `Value`와 `Count`를 계산한다.
2. 공격 데미지 효과(`E_Strike`, `E_Pierce`)면 실행 계층이 `Count`만큼 히트 루프를 돈다.
3. 각 히트마다 공격 애니메이션을 재생하고 짧은 액션 간격을 둔다.
4. 같은 히트에서 `BattleEffectContext.Count = 1`로 효과를 실행해 `Value`만큼만 데미지를 적용한다.
5. 카메라 임팩트를 재생하고 다음 히트 전 짧은 간격을 둔다.
6. 몬스터 스킬도 같은 규칙을 사용한다.

## Testing
- 기존 EditMode 회귀 테스트 파일에 멀티히트 효과 테스트를 추가한다.
- 테스트는 `StrikeEffect`가 `Count = 3`일 때 한 번 호출로 체력을 3회 깎지 않고, 새 단일 히트 실행 경로에서 한 히트만 처리되도록 검증한다.
- 실행 계층의 반복 횟수 계산은 별도 헬퍼로 검증한다.

## Notes
- 시각적 카메라 움직임은 Unity PlayMode가 아니면 완전히 검증하기 어렵다. 자동 테스트는 반복 횟수와 단일 히트 데미지 계약을 검증하고, 실제 카메라/애니메이션은 에디터에서 확인한다.
- 커밋은 프로젝트 지침에 따라 별도 승인 후 진행한다.
