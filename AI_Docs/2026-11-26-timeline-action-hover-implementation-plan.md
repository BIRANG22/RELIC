# Timeline Action Hover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 타임라인 행동 호버 시 상세 툴팁을 표시하고 해당 행동 소유자 아이콘을 전면·완전 불투명으로 임시 강조한다.

**Architecture:** 기존 `TimelineSkillIconHoverUI`를 호버 이벤트의 단일 진입점으로 유지한다. 상세 툴팁은 기존 팝업 View를 확장하고, 소유자 강조는 `BattleTimelineGroupUI`가 현재 엔트리와 소유자 큐를 기준으로 적용·복원한다. 모든 변경은 UI 프레젠테이션에 한정한다.

**Tech Stack:** Unity 6, C#, uGUI, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-26-battle-timeline-record-consumable-redesign.md`의 1단계

## Global Constraints

- 제품 코드보다 테스트를 먼저 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- UI는 예약 명령, `OrderIndex`, 전투 상태를 변경하지 않는다.
- 고정 툴팁 계층은 기존 `TimelinePopupPrefab.prefab`을 수정한다.
- 커밋, Push, PR, 브랜치·worktree 작업은 수행하지 않는다.

## Review Focus

- 비어 있거나 재바인딩된 엔트리 호버 시 이전 툴팁·소유자 강조가 남지 않아야 한다.
- 같은 소유자의 연속 행동 그룹 중 어느 행동을 호버해도 같은 소유자 아이콘이 강조돼야 한다.
- 대기 소유자 강조 중 원래 선두는 대기 알파가 되고 호버 종료 후 선두 알파 1로 복구돼야 한다.
- 슬롯 리바인딩·행동 실행·오브젝트 비활성화 중 호버가 끝나도 형제 순서와 알파가 정상 복구돼야 한다.
- 몬스터와 플레이어 행동 모두 기존 엔트리 설명 데이터로 상세 내용을 표시해야 한다.

---

### Task 1: 상세 툴팁 View

**Files:**
- Modify: `Assets/Project/Scripts/UI/TimelineSkillHoverPopupView.cs`
- Modify: `Assets/Project/Scripts/UI/TimelineSkillHoverPopupUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelinePreviewEntry.cs`
- Test: `Assets/Tests/EditMode~/TimelineSkillHoverPresentationTests.cs`

**Interfaces:**
- Consumes: `BattleTimelinePreviewEntry.SkillName`, `SkillEffectDescription`, `SkillRangeIcon`
- Produces: `TimelineSkillHoverPopupView.Set(string skillName, string effectDescription, Sprite rangeIcon)`

- [x] **Step 1:** 이름·설명·범위 아이콘 바인딩과 빈 설명/아이콘 숨김을 검증하는 실패 테스트를 작성한다.
- [x] **Step 2:** 기존 구현이 설명·범위 인자를 버리는 것을 확인하고 Editor 테스트 컴파일 기반 RED를 기록한다. 프로젝트 규칙상 batchmode 테스트 실행은 하지 않는다.
- [x] **Step 3:** View에 설명 텍스트와 범위 아이콘 직렬화 참조, 자동 바인딩, 콘텐츠 기반 크기 계산을 구현하고 Popup UI가 세 값을 모두 전달하게 한다.
- [x] **Step 4:** 기존 프리팹은 단일 텍스트 호환 경로로 이름과 상세 설명을 줄바꿈 표시한다. 별도 `Description`/`RangeIcon` 계층이 배치되면 자동으로 분리 표시한다.
- [x] **Step 5:** 새 테스트 소스를 강제 포함한 Editor 테스트 어셈블리 빌드로 컴파일 통과를 확인한다.

### Task 2: 소유자 아이콘 임시 강조

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineSkillIconHoverUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Test: `Assets/Tests/EditMode~/TimelineSkillHoverPresentationTests.cs`

**Interfaces:**
- Consumes: `BattleTimelinePreviewEntry.OwnerId`, `IsMonster`, `PlayerCommand`, `MonsterCommand`
- Produces: `BattleTimelineGroupUI.SetHoveredOwner(BattleTimelinePreviewEntry entry)` 및 `ClearHoveredOwner(BattleTimelinePreviewEntry entry)`

- [x] **Step 1:** 큐 인덱스·호버 인덱스별 알파와 표시 순서를 검증하는 실패 테스트를 작성한다.
- [x] **Step 2:** 새 API 부재 상태를 RED로 확인했다.
- [x] **Step 3:** `TimelineSkillIconHoverUI`가 부모 `BattleTimelineGroupUI`에 호버 시작·종료를 전달하게 한다.
- [x] **Step 4:** Group UI가 소유자 키로 큐 항목을 찾고, 강조 아이콘을 최상위 형제로 이동·알파 1로 만들며 나머지를 원래 상대 순서와 대기 알파로 표시하게 한다.
- [x] **Step 5:** 호버 종료, `ClearImmediate`, `SetTimelineEntries`, `OnDisable`, 실행 후 큐 갱신에서 실제 큐 순서와 알파를 복구한다.
- [x] **Step 6:** 런타임 및 Editor 어셈블리를 빌드해 컴파일과 테스트 컴파일을 확인한다.

### Task 3: 정적 자산 및 회귀 검증

**Files:**
- Test: `Assets/Tests/EditMode~/TimelineSkillHoverPresentationTests.cs`

**Interfaces:**
- Consumes: Task 1·2의 공개/정적 계산 인터페이스
- Produces: 없음

- [x] **Step 1:** Battle·DebugBattle 씬이 기존 팝업 프리팹을 계속 참조하는지 정적으로 확인한다.
- [x] **Step 2:** `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [x] **Step 3:** `git diff --check`와 변경 파일 검토로 변경 대상 오류를 확인한다. 기존 사용자 변경 `Record.prefab`의 줄바꿈 경고는 범위 밖으로 보존한다.
- [x] **Step 4:** Unity 에디터 화면에서의 실제 호버 위치·레이아웃·겹침은 직접 제어하지 않고 미검증 항목으로 기록한다.

## 최종 리뷰 반영

- 비연속 동일 소유자 그룹은 `OwnerKey`뿐 아니라 호버 행동의 `OrderIndex`로 정확한 그룹을 선택한다.
- 실행 완료 행동은 호버 대상과 raycast를 즉시 해제해 투명 아이콘의 툴팁 재활성화를 막는다.
- 상세 툴팁에는 행동 종류, 실제 예약 비용, 범위·대상 칸 수와 효과 설명을 함께 표시한다.
- 실행 페이드 중 일시적인 형제 순서 재적용, 사망 색 갱신 중 알파 재적용, 긴 팝업의 부모 기준 화면 경계 보정은 화면 검증이 필요한 Minor로 다음 UI 폴리시 범위에 남긴다.

