# UI·BGM·전투 연출 일괄 버그 수정 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 보고된 15개 UI·오디오·전투 연출 회귀 현상을 기존 상태/프레젠테이션 경계를 유지하며 수정한다.

**Architecture:** BGM 선택은 `BattleBgmDirector`, 대상 추적은 RuntimeId 기반 HUD/호버 컴포넌트, 상태 표시는 UI Presenter가 담당한다. 전투 계산 데이터는 변경하지 않고 표시와 연출 수명주기만 교정한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, NUnit EditMode tests, Unity YAML assets

**Spec:** `AI_Docs/2026-11-15-bugfix-batch-ui-audio-arabella-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 작성한다.
- Unity batchmode 및 에디터 직접 제어를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.
- 전투 결과와 랜덤 로직을 UI/VFX/사운드에서 변경하지 않는다.

## Review Focus

- 메뉴와 튜토리얼이 동시에 열린 경우 입력 차단 및 표시 순서
- 신규 진입과 이어하기 모두에서 이전 씬 BGM이 남지 않는지
- 사망/파괴/비활성 대상의 HUD가 다시 나타나지 않는지
- 몬스터 타임라인 호버 종료가 다른 HUD 소유자의 표시를 끄지 않는지
- 2페이즈와 거미줄 연출이 전투 결과 방향을 바꾸지 않는지

---

### Task 1: 회귀 계약 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`
- Modify: 기존 관련 EditMode 테스트

- [ ] 보고된 데이터·정책·수명주기를 재현하는 실패 테스트를 작성한다.
- [ ] 테스트 어셈블리를 빌드해 예상 실패를 확인한다.

### Task 2: 튜토리얼과 로비 UI

**Files:**
- Modify: 튜토리얼 패널/포커스 관련 코드와 씬·프리팹
- Modify: 공명·연성 패널 관련 코드와 프리팹

- [ ] 메뉴 우선순위와 포커스/툴팁 정렬 정책을 구현한다.
- [ ] 공명 선택 시각 복구와 연성 물음표 폰트를 교정한다.
- [ ] 관련 테스트를 통과시킨다.

### Task 3: BGM 진입 및 루프

**Files:**
- Modify: `Assets/DB/SoundDatabase.asset`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleBgmDirector.cs`
- Modify: 배틀씬 진입 호출부 및 테스트

- [ ] 튜토리얼·2페이즈 루프 데이터를 교정한다.
- [ ] 신규 진입과 이어하기의 초기 BGM 소유권을 통합한다.
- [ ] BGM 정책 테스트를 통과시킨다.

### Task 4: 월드 HUD와 몬스터 정보

**Files:**
- Modify: `WorldFollowHUD`, 몬스터 타임라인 호버, 몬스터 상태 UI
- Modify: 관련 EditMode 테스트

- [ ] 사망/무효 대상 HUD 수명주기를 구현한다.
- [ ] 타임라인 호버에 이름 HUD 표시를 연결한다.
- [ ] 상태명 밑줄과 거미줄 배율 표시 정책을 구현한다.

### Task 5: 아라벨라 및 거미줄 연출

**Files:**
- Modify: 아라벨라 입장·2페이즈 Presenter, 거미줄 설치 연출 및 관련 프리팹/클립
- Modify: 관련 EditMode 테스트

- [ ] 입장 위치/Idle 중복을 제거한다.
- [ ] 거미줄 플립을 확정 방향 기준 한 번으로 제한한다.
- [ ] 2페이즈 상하 VFX 회전을 재생마다 초기화·적용한다.

### Task 6: 전체 검증

- [ ] 런타임 및 에디터 C# 프로젝트 빌드를 실행한다.
- [ ] 관련 EditMode 테스트 소스 계약과 YAML/GUID를 검사한다.
- [ ] `git diff --check`와 변경 범위 검토를 수행한다.
- [ ] 에디터에서만 확인 가능한 항목을 완료 보고에 분리한다.
