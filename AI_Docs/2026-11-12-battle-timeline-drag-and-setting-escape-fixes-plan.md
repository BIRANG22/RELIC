# Battle Timeline Drag And Setting Escape Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 클릭 삭제 복구, 점유 칸 드래그 프리뷰, 자원 부족 드래그 차단, 설정 패널 ESC 우선순위를 구현한다.

**Architecture:** 기존 예약 히스토리와 드롭 API를 유지하면서 사용자 삭제만 복구 가능한 편집으로 기록한다. 드래그 프리뷰는 실제 예약과 분리된 UI 레이아웃 상태로 계산하고, ESC 우선순위는 공용 입력 컨트롤러에서 단일 소유한다.

**Tech Stack:** Unity 6, C#, Unity UI/EventSystem, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-12-battle-timeline-drag-and-setting-escape-fixes-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity batchmode를 실행하지 않는다.
- 드래그 프리뷰는 전투 핵심 상태를 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.

## Review Focus

- 다칸 행동 삭제 복구 후 점유 용량과 순서가 정확히 복원되는가.
- 기존 예약 이동 프리뷰가 자기 자신을 중복 점유로 계산하지 않는가.
- 드래그가 타임라인 밖으로 나가거나 취소될 때 모든 임시 표시가 사라지는가.
- HP·마나·고유 자원·실드 중 어느 하나가 부족해도 신규 드래그가 시작되지 않는가.
- 설정 패널 종료 시 기존 일시정지 복원 규칙이 MenuPanel과 충돌하지 않는가.

---

### Task 1: 복구 가능한 클릭 삭제

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: `RemoveCommand(int, int)`, `RemoveCommandFromNetwork(int, int)`, `RedoLastPlayerReservation()`
- Produces: 사용자 삭제 시 제거 위치를 Redo 기록에 보존하는 경로

- [ ] 실패 테스트로 클릭 삭제 후 동일 명령·슬롯·순서 복원과 다칸 용량 복원을 고정한다.
- [ ] 테스트 컴파일에서 현재 복구 실패를 확인한다.
- [ ] 로컬 사용자 삭제와 네트워크 적용 삭제의 히스토리 의도를 분리하고 기존 비용 재계산을 재사용한다.
- [ ] 런타임 및 에디터 어셈블리 컴파일로 통과를 확인한다.

### Task 2: 점유 칸 드래그 프리뷰

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationDragContext.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationLayoutUtility.cs`
- Test: `Assets/Tests/EditMode~/TimelineReservationLayoutUtilityTests.cs`

**Interfaces:**
- Consumes: `TimelineEntryCost`, 현재 `BattleTimelinePreviewEntry` 목록, 포인터 삽입 위치
- Produces: 실제 예약을 변경하지 않는 임시 배치 목록과 하이라이트 상태

- [ ] 실패 테스트로 1~3칸 신규 삽입, 기존 예약 이동, 자기 자신 제외, 용량 초과를 고정한다.
- [ ] 순수 레이아웃 계산을 최소 구현해 테스트를 통과시킨다.
- [ ] 드래그 이동 시 대상 그룹 프리뷰를 갱신하고 이전 그룹·종료 시 프리뷰를 해제한다.
- [ ] 드롭 성공 때만 기존 `HandleReservationDrop`을 호출하는지 정적 검토한다.

### Task 3: 자원 부족 신규 드래그 차단

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleCharacterPanelTimelineDragTests.cs`

**Interfaces:**
- Consumes: `CanUseSkillWithPreviewResource(SkillMasterData)`
- Produces: 자원이 충분할 때만 `TryBeginTimelineSkillDrag(...)` 성공

- [ ] 각 자원 부족과 충분한 자원 케이스의 실패 테스트를 작성한다.
- [ ] `TryBeginTimelineSkillDrag` 시작 전에 기존 자원 판정을 적용한다.
- [ ] 기존 예약 이동 드래그가 영향받지 않는지 검토한다.

### Task 4: 설정 패널 ESC 우선순위

**Files:**
- Modify: `Assets/Project/Scripts/UI/BattleSettingPanelUI.cs`
- Modify: `Assets/Project/Scripts/BattleMenuEscapeInputController.cs`
- Test: `Assets/Tests/EditMode~/BattleSettingPanelUITests.cs`

**Interfaces:**
- Produces: `BattleSettingPanelUI.TryCloseOpenPanel(): bool`
- Consumes: 공용 ESC 처리 우선순위

- [ ] 활성 설정 패널은 닫고 MenuPanel 토글을 실행하지 않는 실패 테스트를 작성한다.
- [ ] 활성 설정 패널 탐색과 닫기를 표시 컨트롤러에 구현한다.
- [ ] 공용 ESC 처리에서 MenuPanel 분기 전에 호출하고 처리 프레임을 소비한다.
- [ ] 일시정지 복원과 기존 팝업 우선순위 회귀를 검토한다.

### Task 5: 종합 검증

**Files:**
- Review: 위 코드와 테스트 및 관련 씬 YAML 참조

**Interfaces:**
- Consumes: Task 1~4 결과
- Produces: 컴파일 및 정적 검증 결과

- [ ] `Assembly-CSharp.csproj`를 Restore 없이 빌드해 오류 0개를 확인한다.
- [ ] `Assembly-CSharp-Editor.csproj`를 Restore 없이 빌드해 오류 0개를 확인한다.
- [ ] 관련 테스트 소스와 씬/프리팹 YAML 참조를 정적 검토한다.
- [ ] Unity 에디터 화면과 실제 포인터 연출은 미검증 항목으로 분리한다.

### Task 6: 평행사변형 드래그 하이라이트

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Modify: `Assets/Project/PrefabsR/TimelineSlot.prefab`
- Test: `Assets/Tests/EditMode~/TimelineReservationLayoutUtilityTests.cs`

**Interfaces:**
- Consumes: 행동 점유 칸 수 1~3, `slot1_back`/`slot2_back`/`slot3_back` 스프라이트
- Produces: `ResolveReservationDragHighlightSprite(int, Sprite, Sprite, Sprite)` 및 흰색 반투명 하이라이트

- [ ] 점유 칸 1~3이 대응 스프라이트를 선택하는 테스트를 작성한다.
- [ ] `BattleTimelineGroupUI`에 스프라이트와 색상을 serialized reference로 노출한다.
- [ ] `TimelineSlot.prefab`에 기존 turn_sprite 자산을 명시적으로 연결한다.
- [ ] 런타임 및 에디터 어셈블리를 컴파일하고 프리팹 YAML 참조를 정적 검증한다.

