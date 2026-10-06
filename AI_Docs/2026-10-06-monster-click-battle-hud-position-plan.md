# 몬스터 클릭 시 Battle HUD 위치 유지 구현 계획

1. `BattleCharacterPanelUI`에 몬스터 정보 선택 중 HUD 위치 갱신을 억제하는 상태를 추가한다.
2. 몬스터 선택 및 정보 패널 닫힘 이벤트 모두에서 예약된 위치 갱신을 취소한다.
3. MonsterInfoPanel 표시 코루틴에서 Battle HUD 기본 위치 대기를 제거한다.
4. 위치 갱신 진입점에서 몬스터 정보 선택 중에는 이동을 수행하지 않도록 보호한다.
5. EditMode 회귀 테스트와 Unity C# 컴파일로 검증한다.
