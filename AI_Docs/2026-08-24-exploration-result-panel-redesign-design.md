# Exploration Result Panel Redesign Design

## Goal

스테이지 종료 후 표시되는 `ExplorationResultPanel`을 참고 이미지처럼 탐사 보고서 형태로 재구성한다. UI 오브젝트는 `Assets/Project/Scenes/YDM/Battle.unity`에 미리 배치하고, 런타임에는 오브젝트 생성 없이 활성/비활성 및 데이터 바인딩만 수행한다.

## Design

- `ExplorationResultPanelUI`는 단일 TMP 문자열 출력 대신 씬에 배치된 텍스트, 이미지, 슬라이더, 행 오브젝트를 직렬화 필드로 참조한다.
- 패널은 비활성 상태로 씬에 존재하고 `Open`/`OpenDefeat`에서만 활성화된다.
- 캐릭터 행은 최대 3개를 씬에 배치한다. 파티 통계가 부족하면 남은 행은 비활성화한다.
- 캐릭터 초상화는 `CharacterMasterData.Icon` 또는 `CharacterIconDatabase`를 우선 사용한다.
- 획득 레드 더스티움은 `ExplorationResultData.Remnant`를 표시한다.
- 캐릭터 통계는 현재 저장되는 `KillCount`, `DamageDealt`, `DamageTaken`, `DeathCount`를 바인딩한다.
- 전투/사건 요약, 경험치, 해금은 현재 런타임 데이터가 없는 항목이 있으므로 이번 변경에서는 기본값 또는 빈 상태로 표시하며, 실제 기록 데이터가 생기면 같은 필드에 연결한다.

## Multiplayer Boundary

결과 패널은 이미 확정된 `ExplorationResultData`와 런타임 스냅샷만 읽는다. 전투 결과 계산, 보상 판정, 통계 누적 로직은 UI에서 수행하지 않는다.
