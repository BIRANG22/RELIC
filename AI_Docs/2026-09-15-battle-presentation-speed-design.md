# 전투 행동 연출 배속 설계

## 목표

옵션 UI에서 1배, 1.5배, 2배, 3배, 4배를 선택하고 전투 행동 실행 연출에만 적용한다.

## 범위

- 전투 행동 사이의 연출 대기, 이동/공격/피격/사망 애니메이션, 행동으로 생성된 파티클·Animator·VFX Graph를 선택 배율로 재생한다.
- 예약 턴 생성·순서, 전투 상태 변경, 피해 계산, 보상, 랜덤 결과에는 배율을 적용하지 않는다.
- 전역 `Time.timeScale`을 변경하지 않으므로 옵션 UI, 로비, 전투 맵과 이벤트 룸의 일반 흐름은 기본 속도를 유지한다.

## 구조

- `BattlePresentationSpeedSettings`가 선택 인덱스를 PlayerPrefs에 저장하고, 배율 및 연출 시간 축소 값을 제공한다.
- `BattleActionRunner`와 `BattleConsecutiveActionPresentationContext`가 행동 연출의 대기와 프레임 진행만 설정값으로 보정한다.
- `BattleUnitAnimator`와 행동 VFX 생성 지점이 애니메이터, ParticleSystem, VisualEffect의 재생 속도를 보정한다.
- 옵션 패널은 기존 해상도 드롭다운 프리팹 구성을 복제하여 동일한 UI 스타일의 전투 배속 드롭다운을 제공한다.

## 멀티플레이 경계

이 기능은 Command -> State Change -> Result/Event 이후의 로컬 프레젠테이션 시간만 변경하며, 전투 결과와 동기화 대상 ID/스냅샷에는 영향을 주지 않는다.
