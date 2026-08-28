# 이동 경로 호버 화살표 프리뷰 설계

## 목표

이동 스킬 선택 중 그리드에 마우스를 올리면 캐릭터 현재 위치를 제외한 이동 경로 칸에 화살표 타일을 표시한다. 그리드를 클릭해 이동 예약이 완료되거나 선택이 취소되면 경로 프리뷰를 숨긴다.

## 구조

- 전투 계산은 기존 `PlayerSkillReservationController`의 이동 경로 후보를 그대로 사용한다.
- 새 표시 전용 컴포넌트 `MovePathPreview`가 경로 스텝을 받아 인스펙터에 연결된 프리팹을 스폰한다.
- 새 타일 컴포넌트 `MovePathTileView`는 프리팹의 `MeshFilter`, `MeshRenderer`에 직선, 코너, 코너 도착, 끝 메쉬를 생성하고 회전을 적용한다.
- 화살표는 스프라이트가 아니라 단일 면 메쉬로 생성한다. 각 정점은 표시 대상 `GridCell`의 메쉬 UV 네 꼭짓점을 기준으로 보간되어, `GridQuadWarpController`가 만든 그리드 셀의 사다리꼴/원근 형태를 그대로 따른다.
- 메쉬 모양은 회전된 이미지 조각이 아니라 현재 타일의 진입 방향과 탈출 방향으로 생성한다. 직선은 이전 셀 경계에서 다음 셀 경계까지, 코너는 이전 셀 경계에서 중심을 지나 다음 셀 경계까지 이어진다.
- `edgeOverlap` 값으로 셀 경계 밖을 조금 덮어 그리드 이미지 사이의 시각적 틈에서도 선이 끊겨 보이지 않게 한다.
- 일반 코너와 코너 도착은 이미지 기준이 다르므로 별도 회전 테이블을 사용한다. 코너 도착은 필요 시 Y 180도 미러와 Z 회전을 함께 적용한다.
- 경로 스폰 루트는 인스펙터로 지정 가능하게 하고, 비어 있으면 런타임에 `Move Path Preview Root`를 생성한다.
- `MovePathPreview`는 스프라이트 파일 경로를 직접 찾지 않는다. 경로 표시는 `MovePathTile.prefab`의 메쉬 타일 설정을 단일 출처로 사용한다.
- `Battle`, `DebugBattle`, `Battletest` 씬의 `BattleReservationSystem`에 `MovePathPreview`를 붙이고 `PlayerSkillReservationController.movePathPreview`에 명시 연결한다.

## 에셋

- `Assets/Project/Art/Image/MovePath_Straight.png`
- `Assets/Project/Art/Image/MovePath_Corner.png`
- `Assets/Project/Art/Image/MovePath_CornerEnd.png`
- `Assets/Project/Art/Image/MovePath_End.png`
- `Assets/Project/PrefabsR/Battle/MovePathTile.prefab`

기존 PNG는 참조용으로 남기되, 런타임 경로 프리뷰는 대상 그리드 셀 메쉬에 맞춰 워프되는 메쉬 기반 프리팹을 사용한다.

## 경로 표시 교체 방법

- 경로의 색상만 바꾸려면 `Assets/Project/PrefabsR/Battle/MovePathTile.prefab`의 `MovePathTileView.pathColor`를 변경한다.
- 머티리얼이나 텍스처를 바꾸려면 같은 프리팹의 `pathMaterial`에 원하는 머티리얼을 연결한다. 이 머티리얼 하나가 직선, 코너, 코너 도착, 끝 화살표에 공통으로 적용된다.
- 선 두께는 `bodyHalfWidth`, 화살촉 크기는 `arrowHeadLength`, `arrowHeadHalfWidth`, 셀 사이 연결 덮개는 `edgeOverlap`으로 조정한다.
- 현재 구조는 별도 PNG를 파일 경로로 찾아 쓰지 않으므로, 표시 리소스 교체는 프리팹 인스펙터에서 한다.

## 멀티플레이 경계

경로 프리뷰는 이미 계산된 이동 후보를 보여주는 표시 레이어이며 전투 상태, 예약 결과, 랜덤 판정에는 관여하지 않는다.
