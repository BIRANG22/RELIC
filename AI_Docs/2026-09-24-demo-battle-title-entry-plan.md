# 타이틀 데모 전투 진입 구현 계획

1. 데모 파티 구성과 빈 파편/유물 상태를 검증하는 EditMode 회귀 테스트를 먼저 추가한다.
2. 테스트가 대상 타입 부재로 실패하는 것을 컴파일로 확인한다.
3. `DemoBattlePartySetup`을 구현해 `Char_01`, `Char_02`, `Char_04` 런타임과 빈 장비/인벤토리를 구성한다.
4. `TitleDemoBattleButton`을 구현해 `Chapter1 / Stage1` 명령과 고정 seed로 기존 전투 진입 서비스를 호출한다.
5. 타이틀 씬에 기존 버튼과 독립된 데모 버튼을 연결한다.
6. C# 컴파일과 가능한 EditMode 테스트를 검증한다.
