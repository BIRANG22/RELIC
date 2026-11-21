# Tutorial Move Double Click Guard Reapply Plan

**Goal:** 사라진 튜토리얼 이동 버튼 연속 클릭 방어를 회귀 테스트와 함께 재적용한다.

**Spec:** `AI_Docs/2026-11-21-tutorial-move-double-click-reapply-design.md`

## 작업

- [x] `BattleTimelineManualControlTests`에 튜토리얼 재클릭 선택 유지와 일반 전투 재클릭 취소 테스트를 추가한다.
- [x] 새 `allowDeselect` 인터페이스가 없어 테스트 컴파일이 실패하는지 확인한다.
- [x] `ToggleMoveSkillSelection`에 취소 허용 인자를 추가한다.
- [x] 튜토리얼 표현 중 패널 호출에서만 취소를 비활성화한다.
- [x] 회귀 테스트 어셈블리와 프로젝트를 컴파일하고 변경 파일을 정적 검증한다.

커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
