# Monster Info Reused Text Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 재활용된 몬스터 정보 항목의 텍스트가 다른 몬스터를 표시할 때 즉시 새 내용으로 동기화되게 한다.

**Architecture:** 풀 확장에 따른 블러 복제 계층 무효화와 데이터 재바인딩에 따른 즉시 동기화를 분리한다. 기존 복제본은 유지하면서 텍스트·이미지를 동기화하고, 새 항목이 추가된 경우에만 복제 트리를 다시 만든다.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-22-monster-info-reused-text-refresh-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 전투 상태와 멀티플레이 동기화 경계를 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 같은 수량의 스킬 항목을 재사용해도 새 설명이 복제본에 반영되어야 한다.
- 상태 항목 수가 그대로인 값 변경도 즉시 반영되어야 한다.
- 항목 수가 증가하면 새 그래픽이 포함되도록 복제 계층을 재생성해야 한다.
- 항목 수가 증가하지 않으면 복제 계층을 불필요하게 재생성하지 않아야 한다.
- 로케일 변경 후 재바인딩된 문자열도 현재 복제본에 반영되어야 한다.

---

### Task 1: 블러 갱신 결정 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/ReusableComponentPoolTests.cs`

**Interfaces:**
- Consumes: `MonsterInfoBlurRefreshDecision.FromHierarchyGrowth(bool)`
- Produces: 재사용 시 계층 재생성 없이 프레젠테이션을 갱신하고, 풀 확장 시 두 작업을 모두 수행한다는 회귀 보장

- [ ] 계층 미확장 시 `RebuildHierarchy == false`, `RefreshPresentation == true`인 실패 테스트를 작성한다.
- [ ] 계층 확장 시 두 값이 모두 `true`인 실패 테스트를 작성한다.
- [ ] 테스트 어셈블리 컴파일로 테스트 코드의 유효성을 확인한다.

### Task 2: 패널 재바인딩 후 블러 동기화

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs`

**Interfaces:**
- Consumes: 풀 `Prepare`의 계층 확장 결과와 `UIBlurBackgroundManager.RefreshPresentation()`
- Produces: `RefreshBlurReplica(bool hierarchyChanged)`

- [ ] `RefreshBlurReplica`가 `hierarchyChanged`일 때만 소스를 무효화하고 항상 프레젠테이션을 갱신하도록 변경한다.
- [ ] 몬스터 표시, 상태 갱신, 로케일 갱신 호출부에 계층 확장 결과를 전달한다.
- [ ] 런타임 및 에디터 어셈블리를 컴파일한다.
- [ ] diff와 작업 트리를 확인하여 승인 범위 밖 변경이 없는지 검토한다.

### Task 3: 패널 오픈 전체 재바인딩과 동적 텍스트 소유권

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs`
- Modify: `Assets/Project/Scripts/MonsterInfoSkillItemUI.cs`
- Modify: `Assets/Project/Scripts/MonsterInfoStatusItemUI.cs`
- Modify: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: `LocalizedTMPText.ProtectDynamicOwnership(TMP_Text)`
- Produces: 패널 오픈 시 전체 표시 재바인딩과 정적 로컬라이저 덮어쓰기 차단

- [ ] 스킬·상태의 모든 동적 TMP가 `Bind` 시 보호되는 회귀 테스트를 추가한다.
- [ ] 스킬 항목이 현재 `boundSkill`을 저장하도록 수정한다.
- [ ] 몬스터 이름과 스킬·상태 동적 TMP의 소유권을 보호한다.
- [ ] 패널 오픈과 로케일 변경이 동일한 전체 재바인딩 경로를 사용하게 한다.
- [ ] 런타임·에디터 어셈블리 컴파일과 diff를 검증한다.

### Task 4: 패널 활성화 후 현재 몬스터 최종 바인딩

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs`
- Modify: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

**Interfaces:**
- Consumes: 패널 `SetActive(true)`와 현재 몬스터 전체 표시 갱신
- Produces: `MonsterInfoPanelBindingOrder.ActivateThenBind(GameObject, Action)`

- [ ] 활성화 콜백이 이전 이름을 써도 최종 바인딩 값이 남는 실패 테스트를 작성한다.
- [ ] 패널 활성화 이후 현재 몬스터 전체 바인딩을 실행하는 최소 구현을 추가한다.
- [ ] 최종 바인딩 이후 캔버스와 블러 복제본을 동기화한다.
- [ ] 런타임·에디터 어셈블리 컴파일과 diff를 검증한다.
