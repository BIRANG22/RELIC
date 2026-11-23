# Timeline Owner Icon Queue Implementation Plan

**Goal:** 한 슬롯의 행동 소유자 아이콘을 최대 5개까지 겹쳐 표시하고 실제 실행 순서에 맞춰 그라데이션 제거한다.

**Spec:** `AI_Docs/2026-11-23-timeline-owner-icon-queue-design.md`

## 제약

- 전투 상태와 실행 순서는 변경하지 않는다.
- 문서는 `AI_Docs`, 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 직접 제어와 batchmode 테스트는 수행하지 않는다.
- 커밋, Push, PR, 브랜치/worktree 작업은 수행하지 않는다.

## Task 1: 큐 규칙 테스트

- `TimelineOwnerIconQueueTests`에 최대 5개, 순서, 연속 소유자 병합, X 간격, 알파 승격, 실행 순서 제거 테스트를 먼저 작성한다.
- 구현 전 컴파일 실패로 RED를 확인한다.
- 순수 큐 모델을 최소 구현하고 컴파일 GREEN을 확인한다.

## Task 2: UI 및 프리팹 연결

- `TimelineOwnerIconView`가 아이콘 표시, 알파, 독립 그라데이션 머티리얼을 담당한다.
- `BattleTimelineGroupUI`가 최대 5개 뷰를 엔트리 순서로 갱신한다.
- `TimelineSlot.prefab`에 `OwnerIcon01~05`를 고정 배치하고 직렬화 참조를 연결한다.
- X 간격 10과 역순 형제 배치로 01이 가장 앞에 그려지게 한다.

## Task 3: 실행 연출 연결

- `BattleTimelineBarUI`에 슬롯별 소유자 아이콘 실행 연출 API를 추가한다.
- `BattleTimelineController.PlayTimelineIconsAfterActionRoutine`이 기존 실행 `orderIndices`를 전달한다.
- 여러 인덱스가 한 배치에 있으면 함께 제거한다.
- 연속된 같은 소유자 행동은 한 그룹으로 표시하고 마지막 행동에서 그룹 아이콘을 제거한다.
- 몬스터의 `TimelineEntryCost`를 실행 인덱스에도 누적해 UI 엔트리 인덱스와 일치시킨다.

## Task 4: 검증

- 새 테스트와 기존 관련 테스트 소스 컴파일을 확인한다.
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 오류 0을 확인한다.
- 프리팹 YAML의 이름, 참조, 위치, 렌더 순서를 정적 확인한다.

