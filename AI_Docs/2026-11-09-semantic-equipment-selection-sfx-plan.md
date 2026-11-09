# Semantic Equipment and Character Selection SFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 룬 장착과 전투 캐릭터 변경 효과음을 입력 UI가 아니라 성공한 상태 변경에서 재생한다.

**Architecture:** `RuneSettingPanel`은 성공한 장착 후처리를 공통 메서드로 모으고, `BattleTimelineController`는 이전/신규 캐릭터 ID의 의미 있는 변경을 판정해 효과음을 재생한다. 기존 캐릭터 버튼의 직접 클릭음은 `BattleCharacterPanelUI` 바인딩 시 끈다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-09-semantic-equipment-selection-sfx-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- UI/사운드는 전투 결과를 계산하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 동일 룬 슬롯 드롭은 장착음이 없어야 한다.
- 잠금·잘못된 룬·빈 슬롯 없음 등 실패 경로는 장착음이 없어야 한다.
- 초기 `null` 상태에서 자동 선택할 때 캐릭터 변경음이 없어야 한다.
- 같은 캐릭터 재선택에는 변경음이 없어야 한다.
- 다른 유효 캐릭터로 변경할 때 입력 경로와 무관하게 한 번만 재생되어야 한다.

---

### Task 1: 사운드 판정 회귀 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/SemanticSelectionSfxTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RuneSettingPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`

**Interfaces:**
- Produces: `RuneSettingPanel.ShouldPlayRuneEquipSound(bool equipmentChanged)`, `BattleTimelineController.ShouldPlayCharacterChangeSound(string previousCharacterId, string nextCharacterId)`

- [ ] 성공/실패 장착과 초기/동일/다른 캐릭터 선택을 검증하는 실패 테스트를 작성한다.
- [ ] 관련 EditMode 테스트를 실행해 필요한 판정 API 부재로 실패하는지 확인한다.
- [ ] 최소 판정 메서드를 구현한다.
- [ ] 관련 EditMode 테스트를 다시 실행해 통과를 확인한다.

### Task 2: 의미 기반 사운드 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RuneSettingPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

**Interfaces:**
- Consumes: Task 1의 두 판정 메서드
- Produces: 클릭·드래그 공통 룬 장착 완료 처리와 입력 독립적인 캐릭터 변경음

- [ ] 룬 슬롯 갱신 후 저장·아이콘 갱신·이벤트·사운드를 수행하는 공통 완료 메서드를 추가한다.
- [ ] 클릭 장착과 드롭 장착이 공통 완료 메서드를 사용하도록 바꾸고 드롭 전용 사운드를 제거한다.
- [ ] `SelectCharacter`에서 이전 ID를 보존한 뒤 실제 다른 캐릭터 변경 시 효과음을 재생한다.
- [ ] 파티 캐릭터 UI 바인딩 시 해당 루트의 `DBAudioSource` 클릭 재생을 비활성화한다.
- [ ] 관련 테스트와 전체 C# 컴파일을 실행한다.

### Task 3: 최종 검증

**Files:**
- Verify only

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: 검증 근거

- [ ] 변경 diff를 검토해 요청 범위 밖 변경과 기존 사용자 변경 충돌이 없는지 확인한다.
- [ ] EditMode 테스트 가능한 범위와 두 Assembly C# 컴파일을 실행한다.
- [ ] 실제 Unity UI 청음 미검증 사항과 멀티플레이 영향 없음을 보고한다.
