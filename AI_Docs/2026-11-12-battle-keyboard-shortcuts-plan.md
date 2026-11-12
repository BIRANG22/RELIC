# Battle Keyboard Shortcuts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배틀 행동과 예약 편집 단축키를 `F/D/C/Z/X`로 변경한다.

**Architecture:** 공용 키 바인딩 클래스를 단일 소스로 두고 행동 UI와 타임라인 컨트롤러가 각자 소유한 동작만 호출한다. 기존 입력 잠금과 튜토리얼 분기는 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-12-battle-keyboard-shortcuts-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터와 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- `D`가 방향 전환과 다른 동작을 동시에 실행하지 않는가.
- `Z/X`가 메뉴·입력 잠금·튜토리얼 중 실행되지 않는가.
- 첫 전투 제한 분기에서 이동 `F`가 기존 허용 조건을 유지하는가.
- 스킬 `Q/W/E/R` 매핑이 바뀌지 않는가.
- 캐릭터 패널이 비활성인 상황에서도 Undo/Redo가 동작하는가.

---

### Task 1: 공용 키 매핑

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Create: `Assets/Tests/EditMode~/BattleKeyboardShortcutTests.cs`

- [ ] 다섯 신규 행동 키와 네 스킬 키를 고정하는 실패 테스트를 작성한다.
- [ ] 공용 static 키 바인딩을 최소 구현한다.

### Task 2: 행동 UI 연결

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

- [ ] 일반·첫 전투·튜토리얼 분기의 이동/전환/연성제 입력을 공용 매핑으로 교체한다.
- [ ] 기존 버튼 호출과 입력 차단 규칙이 유지되는지 정적 검토한다.

### Task 3: Undo/Redo 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`

- [ ] `Update`에서 Undo/Redo 입력 처리기를 호출한다.
- [ ] `Z/X`가 기존 Undo/Redo 공개 API를 호출하도록 구현한다.
- [ ] 비활성 슬롯 이동 키를 좌우 방향키로 정리한다.

### Task 4: 검증

- [ ] 런타임과 에디터 어셈블리를 Restore 없이 컴파일한다.
- [ ] 키 검색으로 오래된 행동 매핑과 충돌이 없는지 확인한다.
- [ ] Unity 화면 입력은 미검증 항목으로 보고한다.

