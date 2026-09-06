# 결과 패널 경험치 표 기준 정산 및 정렬 구현 계획

1. 기존 결과/경험치 테스트를 표 기준으로 갱신한다.
2. 맵 런타임에서 클리어 방 요약을 만드는 경험치 컨텍스트를 추가한다.
3. `BattleStageClearExperienceService`를 고정 50 보상에서 캐릭터별 표 기반 보상으로 변경한다.
4. 버프 스킬 적용 시 `BattleRunStatisticsRecorder.RecordBuffApplied`를 통해 버프 부여량을 기록한다.
5. `ExplorationResultPanelUI`가 경험치 컨텍스트를 만들고 미리보기/적용에 같은 값을 사용하게 한다.
6. `Battle.unity`의 `ExplorationResultPanel`에 씬 배치 Canvas/GraphicRaycaster를 추가하고 직렬화 값을 갱신한다.
7. EditMode 테스트와 MSBuild로 검증하고, Unity batchmode 테스트는 실행하지 않는다.
