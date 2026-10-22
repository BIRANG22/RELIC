# 몬스터 단계형 행동 연출 구현 계획

1. `BattleUnitActionPresentation`에 Prepare VFX 데이터를 추가한다.
2. Prepare/Action 분리 재생 동작을 검증하는 EditMode 테스트를 먼저 추가하고 실패를 확인한다.
3. `BattleUnitAnimator`에 Prepare 전용 및 Action 전용 API를 구현한다.
4. 기존 자동 Prepare 흐름에서도 Prepare VFX가 재생되도록 연결한다.
5. `ExecuteArabellaLeap`을 이륙 연출, 이동, 착지 연출 순서로 변경한다.
6. 아라벨라 프리팹의 도약 스킬 연출 필드를 새 단계 구조에 연결할 수 있는 상태인지 확인한다.
7. 관련 EditMode 테스트와 C# 컴파일을 검증한다.
