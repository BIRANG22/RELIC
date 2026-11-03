# 로비 더스티움 전환 연출 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 종료 후 레드 더스티움을 1:1 블루 더스티움으로 안전하게 정산하고, 기존 결과 패널 대신 로비 중앙에서 블루 더스티움 HUD로 이동하는 비모달 전환 연출을 재생한다.

**Architecture:** 호스트 권한의 정산 서비스가 재화와 가방 아이템을 연출 전에 원자적으로 확정한다. 로비 연출 조정자는 정산 전·후 값을 받아 실제 데이터와 분리된 HUD 표시값과 씬 배치 UI만 제어한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, Unity Scene YAML, NUnit EditMode

**Spec:** `AI_Docs/2026-11-03-lobby-dustium-conversion-presentation-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- Unity 에디터 직접 제어와 batchmode 테스트를 하지 않는다.
- 실제 정산은 UI/VFX 완료 여부와 분리한다.
- 블루 더스티움과 가방 아이템 변경은 기존 호스트 권한 경계를 유지한다.
- 커밋, Push, PR, 브랜치, worktree 작업을 하지 않는다.

## Review Focus

- 지급 저장 후 연출 도중 종료: 재지급 없이 pending이 정리되는지 Task 2에서 검증한다.
- 레드 더스티움 0 + 가방 아이템 존재: 아이템은 이전되고 연출은 생략되는지 Task 2와 4에서 검증한다.
- HUD가 `OnEnable` 또는 `RefreshAll`로 외부 표시를 덮어쓰는 상황: 잠금 유지 여부를 Task 3에서 검증한다.
- 기존 저장의 옛 환산 필드에 값이 남은 상황: 옛 값은 무시하고 Remnant만 사용하는지 Task 1에서 검증한다.
- 연출 참조 일부 누락: 정산은 유지되고 HUD가 최종 실제값으로 복구되는지 Task 4에서 검증한다.

---

### Task 1: 1:1 pending 보상 생성

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResearchService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/ExplorationResultData.cs`
- Create: `Assets/Tests/EditMode~/ExplorationResearchOneToOneConversionTests.cs`

**Interfaces:**
- Consumes: `ExplorationResultData.Remnant`, `ExplorationResultData.BagItemIds`
- Produces: `ExplorationResearchService.CreatePending(ExplorationResultData, DataManager, float)`가 Remnant 1:1 수량을 포함한 pending 반환

- [ ] 유물·기억과 보상 배율이 있어도 Remnant만 1:1 결과가 되는 실패 테스트를 작성한다.
- [ ] 새 테스트를 임시 MSBuild 타깃으로 포함해 현재 `/2 + 희귀도 환산` 때문에 RED를 확인한다.
- [ ] 희귀도 DB 조회와 `ResearchConversionPolicy` 사용을 제거하고 Remnant 1:1 값만 기록한다.
- [ ] 옛 직렬화 필드는 저장 호환을 위해 유지하되 새 로직의 입력으로 사용하지 않는다.
- [ ] 런타임과 테스트 어셈블리 컴파일로 GREEN을 확인한다.

### Task 2: 원자적 정산 결과와 중복 방지

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/ExplorationResultData.cs`
- Create: `Assets/Tests/EditMode~/PendingDustiumSettlementTests.cs`

**Interfaces:**
- Produces: `PendingResearchSettlementResult` 구조체(적용 여부, 정산 전/후 블루, 전환량)
- Produces: `PendingResearchSettlementService.ApplyOnceWithResult(LobbyRuntimeData)`
- Preserves: `ApplyOnce(LobbyRuntimeData)` 호환 래퍼

- [ ] Remnant 1:1 지급, 가방 이전, 두 번째 호출 무지급, 0 Remnant 아이템 이전, 옛 TotalBlue 무시 테스트를 작성한다.
- [ ] 현재 API 부재 또는 옛 TotalBlue 지급으로 RED를 확인한다.
- [ ] 정산 결과 구조체와 원자적 적용 API를 구현한다.
- [ ] 기존 호출 호환 래퍼가 새 API를 사용하게 한다.
- [ ] 관련 테스트 컴파일 GREEN을 확인한다.

### Task 3: 블루 더스티움 HUD 표시 제어

**Files:**
- Preserve: `Assets/Project/Scripts/UI/LobbyBlueDustiumHudUI.cs`
- Create: `Assets/Tests/EditMode~/LobbyDustiumConversionPresentationTests.cs`

**Interfaces:**
- Consumes: 기존 `SetValueImmediate(int)`와 `Refresh()`
- Produces: 연출 조정자의 HUD 자동 갱신 일시 중지 및 원상 복구 경계

- [ ] 레드 감소와 블루 표시 증가 계산 정책의 실패 테스트를 작성한다.
- [ ] 새 연출 정책 API 부재로 RED를 확인한다.
- [ ] 연출 중 HUD 컴포넌트를 중지하고 `SetValueImmediate`로 표시한 뒤 종료 시 활성 상태와 실제값을 복구한다.
- [ ] 기존 HUD 파일의 인코딩을 변경하지 않고 테스트 컴파일 GREEN을 확인한다.

### Task 4: 결과 패널을 비모달 전환 연출로 교체

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/ResearchResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Create: `Assets/Tests/EditMode~/LobbyDustiumConversionPresentationTests.cs`

**Interfaces:**
- Consumes: `PendingResearchSettlementService.ApplyOnceWithResult`
- Consumes: `LobbyBlueDustiumHudUI` 외부 표시 API
- Produces: 중앙 레드 표시, 이동 링/블러, 색상 보간, HUD 도착 펄스 연출

- [ ] 패널 텍스트/확인 흐름 제거, 호스트 1회 정산, 0 수량 생략, 적용 완료 pending 재지급 방지 정책 테스트를 작성한다.
- [ ] 현재 패널 구조 때문에 RED를 확인한다.
- [ ] `ResearchResultPanelUI`를 비모달 연출 조정자로 축소하고 정산·저장·스냅샷 발행을 연출 시작 전에 완료한다.
- [ ] 누락 참조나 중단 시 HUD 잠금을 해제하고 실제값으로 복구하는 `try/finally` 정리 경계를 구현한다.
- [ ] `Lobby.unity`의 기존 결과 텍스트/버튼 배치를 제거하고 풀스크린 연출 루트, 레드 아이콘/숫자, 이동 효과와 BlueDustiumHUD 참조를 배치한다.
- [ ] 씬 YAML 참조와 테스트 어셈블리 컴파일 GREEN을 확인한다.

### Task 5: 옛 환산 코드 정리와 전체 검증

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/ExplorationResultData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs` (필요한 호출 정리만)
- Test: `Assets/Tests/EditMode~/ExplorationResearchOneToOneConversionTests.cs`
- Test: `Assets/Tests/EditMode~/PendingDustiumSettlementTests.cs`
- Test: `Assets/Tests/EditMode~/LobbyBlueDustiumHudExternalPresentationTests.cs`
- Test: `Assets/Tests/EditMode~/LobbyDustiumConversionPresentationTests.cs`

**Interfaces:**
- Removes runtime use of: `ResearchConversionPolicy`, 유물/기억 환산, 결과 확인 버튼 흐름
- Preserves: 경험치, 가방 아이템 이전, 장비 복원, 전투 런타임 정리, 저장, 로비 씬 전환

- [ ] `ResearchConversionPolicy`와 옛 환산 호출의 잔여 참조를 검사하고 사용되지 않는 정책 코드를 제거한다.
- [ ] `git diff --check`를 실행한다.
- [ ] `Assembly-CSharp.csproj`와 새 EditMode 테스트를 포함한 `Assembly-CSharp-Editor.csproj`를 컴파일한다.
- [ ] 씬 YAML에서 기존 패널 텍스트/확인 버튼 참조가 없고 새 연출 참조가 유효한지 검사한다.
- [ ] Unity 화면의 블러 강도, 곡선 이동과 색 전환 타이밍은 미검증 항목으로 기록한다.

### Task 6: 고정 레드 아이콘과 이동 블러 분리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/ResearchResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Tests/EditMode~/LobbyDustiumConversionPresentationTests.cs`

**Interfaces:**
- Produces: `fixedRedDustiumIcon`은 중앙에 고정되고 수량이 0일 때 숫자와 함께 숨겨짐
- Produces: `transferBlur`만 레드에서 블루로 변하며 BlueDustiumHUD까지 이동

- [ ] 완료 전에는 고정 아이콘이 표시되고 완료 시 숨겨지는 실패 테스트를 작성한다.
- [ ] 정책 API 부재로 RED를 확인한다.
- [ ] 씬에 고정 아이콘과 별도 소프트 원형 블러를 명시적으로 배치하고 직렬화 참조를 분리한다.
- [ ] 이동·색상 변경은 블러에만 적용하고 고정 아이콘은 위치를 변경하지 않는다.
- [ ] 수량 0 도달 시 고정 아이콘과 숫자를 함께 숨긴다.
- [ ] 씬 YAML 참조, `git diff --check`, 런타임과 테스트 어셈블리 컴파일을 확인한다.
