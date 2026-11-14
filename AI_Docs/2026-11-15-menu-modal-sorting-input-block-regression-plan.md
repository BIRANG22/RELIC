# 메뉴 모달 정렬 및 전투 입력 차단 회귀 수정 계획

1. 메뉴 기준 모달 정렬 및 HUD interactable 보존 회귀 테스트를 먼저 추가한다.
2. 메뉴 패널 탐색을 현재 씬과 MenuRoot 범위로 제한하고 명시적 씬 참조를 연결한다.
3. 전체 Selectable 비활성화 로직을 제거하고 배경 raycast blocker만 사용한다.
4. UIManager와 BattleSettingPanelUI가 실제 메뉴 Canvas 기준 정렬 정책을 공유하도록 수정한다.
5. 테스트 컴파일, 런타임 컴파일, YAML 정적 검사를 실행한다.

커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
