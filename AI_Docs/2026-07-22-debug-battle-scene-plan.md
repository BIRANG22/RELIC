# Debug Battle Scene Implementation Plan

**Goal:** `Assets/Project/Scenes/YDM/DebugBattle.unity`에서 실제 BattleRoom 흐름을 유지한 채 유물/룬/효과를 직접 하나씩 테스트할 수 있게 한다.

**Architecture:** 기존 `Battle.unity`를 복제해 씬 환경을 재사용하고, `DebugBattle` 씬에서만 자동 생성되는 런타임 디버그 패널을 추가한다. 테스트 가능한 상태 변경 로직은 별도 서비스로 분리하고, 패널은 그 서비스를 호출만 한다.

**Constraints:**
- 문서는 `AI_Docs` 아래에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 핵심 로직은 UI와 분리하고, 디버그 패널은 기존 런타임 데이터/전투 서비스의 테스트용 호출자 역할만 한다.
- 커밋/PR은 별도 요청 전에는 하지 않는다.

## Tasks

1. `Assets/Tests/EditMode~/BattleEffectDebugToolTests.cs`에 프리셋 목록과 런타임 조작 테스트를 먼저 작성한다.
2. `Assets/Project/Scripts/Debug/BattleEffectDebugTool.cs`에 캐릭터 선택, 유물/룬 장착, HP/코스트/자원 조정, 적 상태 부여, GridEffect 배치, 전투 리로드 헬퍼를 구현한다.
3. `Assets/Project/Scripts/Debug/BattleEffectDebugWindow.cs`에 IMGUI 기반 디버그 패널을 구현한다.
4. `Assets/Project/Scripts/Debug/DebugBattleSceneBootstrap.cs`로 `DebugBattle` 씬에서만 패널을 자동 생성한다.
5. `Assets/Project/Scenes/YDM/Battle.unity`를 `DebugBattle.unity`로 복제하고 새 `.meta`를 만든다.
6. `GameDataRuntime.csv`의 `E_Value` 제거와 EffectId 누락 검증을 재확인하고, MSBuild로 컴파일을 검증한다.
