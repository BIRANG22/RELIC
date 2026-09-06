# 리뷰 피드백 경험치 및 연구 결과 패널 구현 계획

1. 패배 컨텍스트, 누적 경험치, `LV UP` 조건, 로비 연구 결과 패널 비활성 배치를 검증하는 EditMode 테스트를 추가한다.
2. `BattleStageClearExperienceService`가 누적 경험치를 기준으로 preview/apply를 계산하고 레거시 레벨 내 경험치를 보정하도록 수정한다.
3. `ExplorationResultPanelUI`가 패배 결과에서도 컨텍스트와 경험치 미리보기/적용을 수행하도록 수정한다.
4. `ExplorationResultCharacterRowUI`의 `LevelUp` 루트 활성 조건을 실제 레벨업 여부로 제한한다.
5. `ResearchResultPanelUI`에 비활성 씬 패널 자동 오픈 진입점을 추가하고 `Lobby.unity`에서 `ResearchResultPanel` 기본 활성 상태를 끈다.
6. MSBuild, 소스 가드, 씬 YAML 중복 ID 검증을 실행한다.
