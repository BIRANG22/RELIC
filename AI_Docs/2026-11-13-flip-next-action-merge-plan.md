# 플립 다음 행동 결합 구현 계획

**목표:** 플립을 다음 플레이어 행동에 결합하고 용량 및 Undo/Redo를 해당 행동과 공유한다.

**설계:** `AI_Docs/2026-11-13-flip-next-action-merge-design.md`

**기술:** Unity C#, NUnit EditMode, 기존 BattleNetwork DTO/직렬화

## 작업 1: 결합 명령 모델

- `PlayerReservedCommand`에 `FlipBeforeAction`과 설정 API를 추가한다.
- 결합 플립은 행동의 기존 `TimelineEntryCost`를 그대로 사용한다.
- 명령 복제·네트워크 DTO·스냅샷 복원에 필드를 전달한다.
- 모델과 직렬화 테스트를 먼저 작성한다.

## 작업 2: 0칸 임시 플립과 다음 행동 소비

- 기존 플립 클릭·드롭은 0칸 `IsFacingFlipCommand`로 예약하되 히스토리와 타임라인 엔트리에서 제외한다.
- 스킬/이동 명령 확정 시 직전 임시 플립을 `FlipBeforeAction`으로 결합하고 임시 명령을 제거한다.
- 방향 미리보기와 네트워크 스냅샷은 임시 플립 및 결합 플립을 모두 반영한다.
- Undo/Redo에서 결합 명령이 한 단위로 처리되는 테스트를 추가한다.

## 작업 3: 종료 처리와 시각 피드백

- 턴 종료 시 남은 임시 플립을 취소하고 경고한다.
- 결합된 타임라인 행동에 플립 강조를 적용한다.
- 독립 임시 플립은 타임라인 칸을 만들지 않는다.

## 검증

- 대상 EditMode 테스트 소스 포함 컴파일
- `Assembly-CSharp`와 `Assembly-CSharp-Editor` 컴파일
- 씬·프리팹 YAML 변경 없음 확인
