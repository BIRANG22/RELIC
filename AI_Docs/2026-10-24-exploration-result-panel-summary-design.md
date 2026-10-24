# 정산패널 방문 횟수·침식 레벨·아이콘 설정 설계

## 목표

- 정산패널의 일반 전투, 정예 전투, 사건 조우 횟수를 실제 노드 입장 기록으로 표시한다.
- `ExplorationReportErosionIcon` 옆에 선택 침식 점수 합계를 `Lv. N` 형식으로 표시한다.
- 런타임 생성 아이콘의 크기와 배치 값을 인스펙터에서 직접 조절할 수 있게 한다.

## 조사 결과

- 기존 노드 횟수는 `BattleStageClearExperienceContext`의 클리어 횟수를 재사용한다. 이 값은 경험치 계산용 `ClearedMapIds` 기반이라 입장 후 미클리어한 노드가 빠진다.
- 실제 입장 기록은 `MapRuntimeData.VisitedMapIds`에 안정적인 노드 인덱스 문자열로 저장된다.
- 침식 점수 합계 계산은 이미 존재하지만 `stageResultText`가 Battle 씬에서 연결되지 않아 표시되지 않는다.
- 선택 침식, 획득 아이템, 캐릭터 유물·스킬 슬롯의 크기와 간격이 코드 상수로 고정되어 있다.

## 설계

### 방문 노드 집계

`ExplorationNodeVisitSummary`를 순수 계산 구조로 추가한다. `VisitedMapIds`를 중복 제거한 뒤 `GeneratedNodes`의 `NodeIndex`와 대응시키고 타입을 다음과 같이 집계한다.

- `Common`, `Battle`: 일반 전투
- `Elite`: 정예 전투
- `Special`, `Event`: 사건 조우
- 그 외 타입: 현재 정산패널 표시 대상에서 제외

UI는 계산 결과만 표시하며 전투 상태를 변경하지 않는다.

### 침식 레벨 표시

Battle 씬의 `ExplorationReportErosionIcon` 옆에 `ExplorationReportErosionLevel` TMP 텍스트를 배치하고 `ExplorationResultPanelUI.erosionLevelText`에 명시적으로 연결한다. 값은 선택된 침식 데이터의 `Score` 합계를 `Lv. N`으로 표시한다.

### 아이콘 배치 설정

`ExplorationResultPanelUI`에 선택 침식과 획득 아이템 슬롯의 크기, 간격, 열 개수를 직렬화 필드로 노출한다. `ExplorationResultCharacterRowUI`에는 유물과 스킬 슬롯의 크기, 간격, 열 개수를 노출한다. 고정 대표 아이콘은 기존처럼 씬 RectTransform의 Width/Height로 조정한다.

## 검증

- EditMode 테스트로 방문 기록과 클리어 기록이 다를 때 방문 횟수가 사용되는지 확인한다.
- 중복·잘못된 방문 ID와 지원하지 않는 노드 타입을 안전하게 무시하는지 확인한다.
- Battle 씬에 침식 레벨 텍스트 및 직렬화 연결이 존재하는지 확인한다.
- C# 프로젝트 컴파일로 직렬화 필드와 호출부를 검증한다.

## 멀티플레이 경계

전투 상태나 결과 계산은 변경하지 않는다. 이미 저장된 안정적인 `NodeIndex` 방문 ID를 읽어 정산 화면용 결과로 투영하므로 네트워크 프레임워크나 Scene Object 동기화 의존성을 추가하지 않는다.
