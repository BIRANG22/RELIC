# 아라벨라 페이즈 전환 VFX 설계

## 목표

- 아라벨라가 2페이즈로 전환되어 `2phase` 애니메이션을 시작할 때 지정된 VFX를 동시에 한 번 재생한다.
- VFX가 지정되지 않은 경우 기존 페이즈 전환 애니메이션과 전투 진행을 그대로 유지한다.

## 구조

전투 핵심 상태 전환은 기존 흐름을 유지한다.

`페이즈 전환 State Change -> PlayArabellaPhaseTransition 결과 표현 -> 2phase 애니메이션 + 전환 VFX`

- `BattleUnitAnimator`에 `BattleVfxEntry` 타입의 페이즈 전환 VFX 슬롯을 추가한다.
- `PlayArabellaPhaseTransition()`이 최초 호출될 때 애니메이션 시작과 함께 기존 `SpawnVfx()` 경로로 VFX를 재생한다.
- 중복 호출되더라도 한 전투 개체의 페이즈 전환 VFX는 한 번만 생성한다.
- 위치, 방향 반전, 정렬, 재생 속도 및 VFX 사운드는 기존 전투 VFX 정책을 재사용한다.
- 애니메이션 이벤트나 VFX 완료 여부는 페이즈 전환 상태와 전투 진행을 결정하지 않는다.

## 프리팹 작업

- `Arabella.prefab`의 `BattleUnitAnimator > Phase Transition VFX` 슬롯을 노출한다.
- 실제 VFX 리소스가 확정되지 않았으므로 슬롯은 비워 두며, 기획·아트가 인스펙터에서 연결한다.

## 검증

- 페이즈 전환 메서드를 두 번 호출해도 VFX가 한 번만 생성되는지 EditMode 테스트로 검증한다.
- VFX가 비어 있는 기존 프리팹 직렬화가 안전한지 확인한다.
- C# 프로젝트 컴파일로 코드 연결을 검증한다.

## 멀티플레이 경계

VFX는 이미 확정된 `Mon_12_1 -> Mon_12_2` 상태 전환 결과만 표현한다. 전투 판정, 런타임 ID, 랜덤 및 동기화 상태에는 영향을 주지 않는다.
