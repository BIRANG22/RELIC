# 아라벨라 IdleVFX 및 2페이즈 Idle 수정 설계

## 원인

- 지속 IdleVFX는 `SpriteRoot`가 아닌 형제 `VfxSpawnPoint`를 추적하며, 방향 보정은 생성 순간에만 적용된다. 이후 아라벨라가 플립해도 기존 인스턴스는 갱신되지 않는다.
- 사냥 강습은 의도적으로 `SpriteRoot`만 이동시키므로 루트 기준 IdleVFX는 출발 위치에 남는다.
- Animator의 `PhaseOneDefeated` 상태가 `Idle.anim`을 참조하여 2페이즈 전환 직전에 1페이즈 Idle이 노출된다. 런타임의 일반 `PlayIdle()`은 2페이즈에서 정상적으로 `Idle_b`를 선택한다.

## 수정

- `BattleUnitAnimator`가 지속 IdleVFX 생성 당시 방향을 기억하고, 방향이 달라지면 현재 페이즈의 VFX를 새 방향으로 재생성한다.
- 지속 IdleVFX에 중첩 가능한 일시 정지 API를 추가한다. 사냥 강습 점프 구간을 `try/finally`로 감싸 점프 시작 시 숨기고 착지·중단 시 복구한다.
- `Arabella_BattleAnimatorController`의 `PhaseOneDefeated` 모션을 `Idle.anim`에서 `Dead.anim`으로 교체한다.

## 비목표

- 모든 전투 VFX의 부모 구조를 변경하지 않는다.
- 점프 시 IdleVFX 자체를 별도 궤적으로 이동시키지 않는다.
- 2페이즈 상태 이름 매핑이나 전투 상태 계산은 변경하지 않는다.

## 안전성

- VFX 변경은 표시 전용이며 전투 결과에 영향을 주지 않는다.
- 일시 정지는 카운터 방식으로 구현해 중첩 호출과 코루틴 중단에도 안전하게 복구한다.
- 기존 `idleVfx`/`phaseTwoIdleVfx` 인스펙터 데이터 형식은 유지한다.
