# RestRoom CSV 기반 Map ID 해소 계획

1. Rest CSV 데이터가 없을 때 하드코딩 ID가 생성되지 않는 회귀 테스트를 추가한다.
2. `ManualBattleMapTemplate`의 Start/Rest 폴백 코드를 제거한다.
3. `BattleMapPanel` 생성 키 버전을 갱신해 기존 런타임 맵을 재생성한다.
4. 컴파일 및 Map_26 연결을 확인한다.
