# Boss Result Panel Canvas, Stats, And Experience Design

## Goal

보스방 결과 패널은 전투방 루트에 묶이지 않고 루트 Canvas 아래에 씬 배치로 존재해야 한다. 보스 치트 클리어처럼 전투 기록이 없는 경우에도 파티 캐릭터 row는 유지되고 전투 통계는 0으로 표시되어야 한다.

## Design

- `ExplorationResultPanel` RectTransform의 부모를 `BattleHUDCanvas`에서 루트 `Canvas`로 옮긴다.
- `ExplorationResultBuilder`는 `BattleRuntimeData.LobbyLoadoutSnapshots`를 기준으로 캐릭터 통계 row를 먼저 만들고, 실제 전투 통계가 있으면 같은 `CharacterId`에 병합한다.
- 버프 부여량은 현재 별도 기록 경로가 없으므로 `BattleRunCharacterStatisticsData.BuffApplied`를 추가하고 기본값 0을 표시한다.
- 스테이지 클리어 경험치는 전투 통계와 분리된 `BattleStageClearExperienceService`에서 캐릭터 런타임에 동일량을 적용한다.
- 결과 패널 캐릭터 이미지는 `PlayerHUDSlot`과 같은 `CharacterIconDatabase.TryGetSideImage`를 우선 사용하고, 없을 때 portrait/icon으로 fallback한다.

## Multiplayer Boundary

결과 데이터 생성은 `BattleRuntimeData`의 안정적인 `CharacterId`와 로비 로드아웃 스냅샷을 기준으로 처리한다. UI는 결과를 표시하고 귀환 시 정산 서비스를 호출하며, 전투 판정이나 랜덤 결과에는 관여하지 않는다.
