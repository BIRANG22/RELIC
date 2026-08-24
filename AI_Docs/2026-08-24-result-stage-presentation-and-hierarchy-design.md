# 결과 패널 스테이지 표시 및 계층 정리 설계

## 목표

- `ExplorationReportStageName`에 클리어한 스테이지의 표시 이름을 넣는다.
- `ExplorationReportStagePreview`에 클리어한 스테이지의 프리뷰 이미지를 넣는다.
- 이름과 이미지는 결과 패널 인스펙터에서 직접 연결할 수 있게 한다.
- `ExplorationReportFrame`의 자식들을 역할별 그룹으로 묶어 씬 계층을 정리한다.

## 스테이지 표시

- `ExplorationResultPanelUI`에 `stagePresentations` 직렬화 리스트를 추가한다.
- 각 항목은 `Stage`, `DisplayName`, `PreviewSprite`를 가진다.
- `GeneratedMapNodeData.MapId`로 데이터시트의 `MapData`를 찾고, 그 안의 `Stage` 값을 표시 매칭 키로 사용한다.
- 매칭 우선순위는 `Stage` 정확히 일치, `MapData.Name`, `MapData.Stage`, `MapId`, 기본 문구 순서로 한다.
- 프리뷰 스프라이트가 있으면 `stagePreviewImage.sprite`에 넣고 표시한다.
- 매칭 스프라이트가 없으면 기존 프리뷰 이미지를 지워 잘못된 이전 이미지가 남지 않게 한다.

## 씬 계층

- `ExplorationReportTopGroup`: 제목, 레드 더스티움 라벨/아이콘/값, 상단 라인.
- `ExplorationReportStageGroup`: 스테이지 이름, 클리어/철수, 프리뷰 이미지, 방 요약 라벨/값.
- `ExplorationReportTableHeaderGroup`: 표 구분선과 헤더 텍스트.
- `ExplorationReportRows`: 기존 캐릭터 행 그룹 유지.

## 경계

- 표시 이름과 이미지는 UI 표현 데이터이며 전투 결과 계산에 관여하지 않는다.
- 전투 결과/경험치 계산 서비스는 수정하지 않는다.
