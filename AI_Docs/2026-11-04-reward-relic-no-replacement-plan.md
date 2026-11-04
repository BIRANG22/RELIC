# Reward Relic No Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 보상 유물을 빈 슬롯에만 장착하고 미사용 통합 보상 패널을 제거하며 Pierce 가시 반사 연결을 복구한다.

**Architecture:** `BattleRewardRelicEquipPolicy`가 액티브/패시브 빈 슬롯 선택을 순수 계산하고 실제 유물 패널이 이를 사용한다. 보상 호출자는 실제 유물/기억 패널을 직접 호출하며 미사용 `BattleRewardEquipPanelUI` MonoBehaviour는 제거한다. Pierce 복구는 현재 Impact 기록을 보존한 채 Ward 반응 호출만 다시 연결한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-04-reward-relic-no-replacement-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터와 batchmode 테스트를 실행하지 않는다.
- 로비 유물 해제/교환은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 점유된 패시브 슬롯 클릭이 기존 유물을 덮어쓰지 않는가.
- 패시브 유물은 1~6번 중 첫 빈 슬롯에만 들어가는가.
- 액티브 유물은 0번 전용 슬롯만 사용하는가.
- 모든 빈 슬롯이 소진되면 장착이 실패하고 추출 경로가 남는가.
- 기존 SkillImpact 기록을 유지하면서 Pierce 가시 반사가 복구되는가.

---

### Task 1: 빈 슬롯 전용 보상 유물 정책

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/BattleRewardRelicEquipPolicy.cs`
- Modify: `Assets/Project/Scripts/BattleRewardRelicPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleRewardRelicEquipPolicyTests.cs`

- [ ] 정책 API가 없는 상태에서 실패하는 테스트 작성 및 RED 확인
- [ ] 액티브/패시브 첫 빈 슬롯 계산 구현
- [ ] 실제 유물 패널에서 점유 슬롯 교체 분기 제거

### Task 2: 미사용 보상 패널 제거 및 실제 패널 직접 연결

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardSkillPanelUI.cs`
- Modify: 보상 호출자 4개와 `BattleRewardPanelUI.cs`, `BattleUIBlurRootCollector.cs`
- Delete: `Assets/Project/Scripts/BattleRewardEquipPanelUI.cs` 및 `.meta`
- Test: `Assets/Tests/EditMode~/BattleRewardPanelRoutingTests.cs`

- [ ] 실제 패널 직접 호출과 구형 타입 참조 부재를 검증하는 실패 테스트 작성
- [ ] 기억 패널 정적 진입점 추가 및 호출자 전환
- [ ] fallback 필드/분기/Blur 참조 제거 후 미사용 파일 삭제

### Task 3: Pierce 가시 반사 복구

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/PierceEffect.cs`
- Test: 기존 `Assets/Tests/EditMode~/BattleWardReactionServiceTests.cs`

- [ ] 현재 Pierce의 Ward 호출 부재를 확인
- [ ] 현재 Impact 기록을 보존하며 피격 전 가시 캡처와 피해 후 반사 호출 복구
- [ ] 런타임/에디터 어셈블리 컴파일 및 정적 검증
