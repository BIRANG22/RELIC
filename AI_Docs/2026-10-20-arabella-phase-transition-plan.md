# 아라벨라 2페이즈 전환 구현 계획

1. 아라벨라 페이즈 전환 규칙을 검증하는 EditMode 테스트를 먼저 작성한다.
2. ID 기반 `ArabellaPhaseTransition` 상태 변환 로직을 구현한다.
3. `BattleDeathService`가 일반 사망 처리 전에 1페이즈 전환을 적용하도록 연결한다.
4. `BattleUnitAnimator`에 2페이즈 전환 및 아라벨라 2페이즈 상태명 선택을 추가한다.
5. `Battlemap_16`의 스폰 ID를 `Mon_12_1`로 수정한다.
6. EditMode 테스트와 C# 프로젝트 컴파일로 검증한다.
7. `2phase` 연출의 Idle 복귀와 K 디버그의 조건부 전투 종료를 보완한다.
8. `EliseAI`의 구형 스킬 ID를 현재 데이터 ID로 교체하고 런타임 보유 스킬로 예약을 제한한다.

