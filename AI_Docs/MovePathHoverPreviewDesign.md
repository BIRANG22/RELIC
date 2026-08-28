# 이동 경로 호버 화살표 프리뷰 설계

## 목표

이동 스킬 선택 중 그리드에 마우스를 올리면 캐릭터 현재 위치를 제외한 이동 경로 칸에 화살표 타일을 표시한다. 그리드를 클릭해 이동 예약이 완료되거나 선택이 취소되면 경로 프리뷰를 숨긴다.

## 구조

- 전투 계산은 기존 `PlayerSkillReservationController`의 이동 경로 후보를 그대로 사용한다.
- 새 표시 전용 컴포넌트 `MovePathPreview`가 경로 스텝을 받아 인스펙터에 연결된 프리팹을 스폰한다.
- 새 타일 컴포넌트 `MovePathTileView`는 프리팹에 연결된 직선, 코너, 코너 도착, 끝 스프라이트와 회전을 적용한다.
- 일반 코너와 코너 도착은 이미지 기준이 다르므로 별도 회전 테이블을 사용한다. 코너 도착은 필요 시 Y 180도 미러와 Z 회전을 함께 적용한다.
- 경로 스폰 루트는 인스펙터로 지정 가능하게 하고, 비어 있으면 런타임에 `Move Path Preview Root`를 생성한다.
- `MovePathPreview`는 스프라이트 파일 경로를 직접 찾지 않는다. 타일 이미지는 `MovePathTile.prefab`의 인스펙터 참조를 단일 출처로 사용한다.
- `Battle`, `DebugBattle`, `Battletest` 씬의 `BattleReservationSystem`에 `MovePathPreview`를 붙이고 `PlayerSkillReservationController.movePathPreview`에 명시 연결한다.

## 에셋

- `Assets/Project/Art/Image/MovePath_Straight.png`
- `Assets/Project/Art/Image/MovePath_Corner.png`
- `Assets/Project/Art/Image/MovePath_CornerEnd.png`
- `Assets/Project/Art/Image/MovePath_End.png`
- `Assets/Project/PrefabsR/Battle/MovePathTile.prefab`

## 멀티플레이 경계

경로 프리뷰는 이미 계산된 이동 후보를 보여주는 표시 레이어이며 전투 상태, 예약 결과, 랜덤 판정에는 관여하지 않는다.
