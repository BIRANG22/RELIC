# Glyph Flip Undo Boss BGM Phase2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 물음표 글리프, 플립 Undo 정리, 보스 이어하기 BGM, 아라벨라 2페이즈 BGM 타이밍을 수정한다.

**Architecture:** 전투 결과 로직은 유지하고 폰트 에셋, 예약 편집 후처리, BGM 상태 선택, 사운드 연출 이벤트를 각 소유 계층에서 수정한다. BGM 전환 시간은 기존 AudioManager의 이중 뱅크 구조를 재사용한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, uGUI, NUnit EditMode

**Spec:** `AI_Docs/2026-11-14-glyph-flip-undo-boss-bgm-phase2-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트는 수행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- fallback 물음표가 두 DungGeunMo 에셋 모두에서 검색되는지 확인한다.
- Undo가 삭제한 행동의 redo 가능성을 유지하면서 미병합 플립만 제거하는지 확인한다.
- 새 전투 맵 진입의 기본 BGM이 사라지지 않는지 확인한다.
- 2페이즈 연출이 중단돼도 기존 BGM과 2페이즈 BGM이 겹쳐 남지 않는지 확인한다.
- fade duration 0일 때 즉시 정지·재생되는지 확인한다.

---

### Task 1: DungGeunMo 물음표 fallback

**Files:** 두 DungGeunMo TMP 에셋, `Assets/Tests/EditMode~/FontQuestionMarkFallbackTests.cs`

- [ ] 두 폰트의 직접 U+003F 제거와 fallback 동작 테스트를 작성한다.
- [ ] 테스트가 현재 잘못된 직접 매핑으로 실패하는지 확인한다.
- [ ] 두 에셋을 수정하고 테스트 어셈블리 컴파일을 확인한다.

### Task 2: Undo 후 미병합 플립 정리

**Files:** `BattleTimelineController.cs`, `BattleTimelineManualControlTests.cs`

- [ ] 행동 Undo 뒤 플립이 남지 않는 회귀 테스트를 작성한다.
- [ ] 성공 Undo 공통 후처리에 플립 정리를 구현한다.
- [ ] redo 히스토리 보존과 컴파일을 확인한다.

### Task 3: 보스방 이어하기 BGM

**Files:** `BattleState.cs`, `ContextualBgmTests.cs`

- [ ] 현재 미완료 노드 유무에 따른 기본 BGM 정책 테스트를 작성한다.
- [ ] 이어하기 노드가 있을 때 `Stage1Common` 재생을 생략한다.

### Task 4: 아라벨라 2페이즈 BGM 시퀀스

**Files:** `AudioManager.cs`, `ArabellaPhaseTransition.cs`, `BattleBgmDirector.cs`, 관련 EditMode 테스트

- [ ] 연출 시작·완료 이벤트와 fade 정책 테스트를 작성한다.
- [ ] 호출별 BGM fade 시간과 BGM 단독 fade-out API를 구현한다.
- [ ] 연출 시작 fade-out, 완료 fade-in 연결 및 Inspector duration을 추가한다.
- [ ] 전체 런타임·에디터 어셈블리 컴파일과 diff를 검증한다.
