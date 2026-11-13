# 아라벨라 페이즈별 프리뷰 수정 설계

## 조사 결과

- 몬스터 클릭 프리뷰는 현재 `MonsterUnit.RuntimeData.MonsterId`로 `MonsterPrefabDatabase` 항목을 조회한다.
- 아라벨라의 페이즈 전환 시 런타임 데이터는 `Mon_12_1`에서 `Mon_12_2`로 정상 교체된다.
- `Mon_12_1`과 `Mon_12_2`의 `idlePreviewSprites`가 모두 1페이즈 스프라이트를 참조하고 있어 2페이즈에서도 같은 프리뷰가 표시된다.
- 2페이즈 Idle 리소스 `Mon_B_01_b_idle.png`와 6개 프레임은 이미 프로젝트에 존재한다.

## 권장 설계

- 코드에 아라벨라 전용 분기를 추가하지 않는다.
- `MonsterPrefabDatabase`의 `Mon_12_2.idlePreviewSprites`만 기존 2페이즈 Idle 6프레임으로 교체한다.
- 런타임 MonsterId 기반 조회 구조를 유지해 이후 다른 다중 페이즈 몬스터에도 동일한 데이터 방식이 적용되게 한다.
- EditMode 데이터 검증 테스트로 두 페이즈가 서로 다른 프리뷰를 사용하며 2페이즈가 `_b_idle_` 프레임을 참조하는지 확인한다.

## 멀티플레이 경계

프리뷰 스프라이트는 UI 표현 데이터이므로 전투 상태 및 동기화 결과에는 영향을 주지 않는다. 페이즈 상태는 기존 런타임 MonsterId를 그대로 사용한다.
