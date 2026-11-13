# 스킬 자원 제한 및 플립 드래그 수정 구현 계획

> **실행 지침:** 현재 작업 공간에서 TDD 순서로 직접 구현한다. 커밋은 수행하지 않는다.

**목표:** 자원 부족 스킬 드래그 회귀를 복구하고 플립 버튼의 타임라인 드래그 예약을 지원한다.

**구조:** UI는 드래그 의도만 만들고 `TimelineReservationDragContext`가 행동 종류를 전달한다. 실제 플립 명령 생성과 확정은 `BattleTimelineController`가 기존 예약 경로에서 담당한다.

**기술:** Unity C#, EventSystems, NUnit EditMode 테스트

**설계:** `AI_Docs/2026-11-13-skill-flip-drag-fix-design.md`

## 제약

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- 프리팹·씬과 네트워크 프로토콜은 변경하지 않는다.
- Unity batchmode는 실행하지 않고 C# 컴파일로 검증한다.

### 작업 1: 드래그 정책 회귀 복구

**파일:**
- 수정: `Assets/Tests/EditMode~/BattleCharacterPanelTimelineDragTests.cs`
- 수정: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

- [ ] 기존 누락 메서드 때문에 테스트/컴파일이 실패하는지 확인한다.
- [ ] 미리보기의 HP, Cost, 고유 자원, Shield 복합 비용을 검사하는 테스트를 보강한다.
- [ ] `CanBeginNewSkillDrag`와 `CanAffordPreviewCosts`를 복구하고 실제 드래그 시작에 연결한다.
- [ ] 컴파일로 통과를 확인한다.

### 작업 2: 플립 드래그 컨텍스트와 예약 연결

**파일:**
- 수정: `Assets/Tests/EditMode~/BattleCharacterPanelTimelineDragTests.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationDragContext.cs`
- 수정: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`

- [ ] 플립 컨텍스트가 일반 스킬과 구분되고 1칸을 점유하는 실패 테스트를 작성한다.
- [ ] `BeginNewFacingFlip`과 `IsFacingFlip`을 최소 구현한다.
- [ ] 플립 버튼 드래그 소스와 시작 함수를 연결한다.
- [ ] 드롭 시 대상 슬롯을 선택하고 기존 플립 명령 예약 함수를 호출한다.
- [ ] 전체 런타임/에디터 어셈블리 컴파일로 검증한다.

## 검토 초점

- 예약된 보조 비용 때문에 주 자원 표시만으로는 사용 가능해 보이는 경우
- HP 비용이 현재 HP와 같아 사망하게 되는 경우
- 드래그 입력 잠금 중 컨텍스트가 생성되는 경우
- 플립 드롭이 일반 이동 경로 선택을 시작하는 경우
- 플립 드롭 실패가 기존 예약을 변경하는 경우
