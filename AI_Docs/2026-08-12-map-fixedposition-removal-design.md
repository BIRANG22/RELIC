# FixedPosition Runtime Removal Design

## Goal

고정맵 템플릿을 기준으로 맵 노드의 위치, 타입, 연결을 직접 결정하므로 `MapData.FixedPosition`을 런타임 맵 선택 기준에서 제거한다.

## Current Problem

기존 절차형 맵 생성은 시작, 보스, 일반 후보를 구분하기 위해 `FixedPosition.Front`, `FixedPosition.Final`, `FixedPosition.None`을 사용한다. 수동 템플릿 도입 후에는 노드의 `Type`과 연결 정보가 이미 템플릿에 있으므로, `FixedPosition` 값이 남아 있으면 엑셀 정리 후 Start/Boss 선택이 실패하거나 불필요한 데이터 관리가 생긴다.

## Design

- `MapData`에서 `FixedPosition` 필드와 enum을 제거한다.
- 수동 템플릿의 빈 `MapIdOverride`는 `Chapter`, `Stage`, `Type`만으로 후보 맵을 고른다.
- `Start` 타입은 기존처럼 해당 맵 데이터가 없어도 `"Start"` 가상 맵으로 fallback한다.
- `Boss` 타입은 `FixedPosition.Final`이 아니라 `Type == "Boss"` 후보를 사용한다.
- 절차형 fallback 생성기도 같은 기준을 사용해 `Type`만으로 맵을 고른다.
- 후보가 없을 때 아무 맵이나 고르는 fallback은 Start/Boss를 피하고, 실제 플레이 가능한 방 타입만 대상으로 한다.

## Out Of Scope

- 엑셀 파일의 `FixedPosition` 컬럼 삭제 또는 값 정리는 이번 작업에서 하지 않는다.
- 이벤트 ID 연동 로직 추가는 별도 작업으로 둔다.
- 맵 템플릿 에셋 구조 변경은 하지 않는다.

## Testing

- 수동 템플릿에서 빈 Start/Boss/Common 노드가 `FixedPosition` 없이 `Type` 기준으로 해석되는지 EditMode 테스트로 확인한다.
- 기존 수동 템플릿 테스트 fixture에서 `FixedPosition` 값을 제거한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않는다.
- 컴파일 검증은 MSBuild로 수행한다.

## Multiplayer Impact

맵 생성 결과를 만드는 데이터 선택 기준만 바뀐다. 전투 중 UI/VFX/사운드나 Scene Object 직접 상태 변경은 추가하지 않으며, 기존 `BattleRandom` 기반 선택 흐름을 유지한다.
