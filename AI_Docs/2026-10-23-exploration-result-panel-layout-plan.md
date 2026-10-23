# Exploration Result Panel Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 탐사 정산 패널을 레퍼런스 배치에 맞추고 동적 텍스트 덮어쓰기와 상세 정보 호버 문제를 해결한다.

**Architecture:** Battle 씬에 고정 UI 구조와 명시적 참조를 배치하고 `ExplorationResultPanelUI` 및 `ExplorationResultCharacterRowUI`는 DB 기반 데이터 표시만 담당한다. 상세 정보는 정산패널 최상위 공용 오버레이로 표시하고, 로비 난이도 침식은 선택 ID에서 총점과 단계별 아이콘을 구성한다.

**Tech Stack:** Unity 6, C#, UGUI, TextMeshPro, NUnit EditMode 테스트

**Spec:** `AI_Docs/2026-10-23-exploration-result-panel-layout-design.md`

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 보상·전투 결과·맵 진행 상태는 변경하지 않는다.
- 아이콘은 기존 DB와 씬 리소스를 사용한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 동적 플레이타임과 로비 난이도 침식 점수에 고정 로컬라이징 값이 다시 적용되지 않아야 한다.
- 캐릭터가 1~2명뿐인 경우 비어 있는 행과 툴팁이 노출되지 않아야 한다.
- 아이콘 ID가 없거나 DB 조회가 실패해도 빈 슬롯 때문에 예외가 발생하지 않아야 한다.
- 호버 후 포인터가 빠져나가면 고정하지 않은 툴팁이 닫혀야 하며 클릭 고정 상태는 해당 아이콘에서만 전환되어야 한다.
- 해상도 비율이 달라도 공용 오버레이가 캐릭터 행을 밀거나 패널 밖으로 크게 벗어나지 않아야 한다.
- 전투 중 누적 침식도와 로비에서 선택한 난이도 점수를 혼동하지 않아야 한다.
- 로비 아이콘 캐시가 없는 재개·디버그 진입에서도 DB fallback 아이콘이 표시되어야 한다.

---

### Task 1: 로비 난이도 침식 요약

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `BattleRuntimeData.ExplorationElapsedSeconds`, `BattleRuntimeData.SelectedErosionDifficultyIds`, `ErosionDatabase`, `ExplorationResultData.Remnant`
- Produces: 플레이타임, 선택 난이도 총점·단계별 아이콘, 레드더스티움 및 획득 아이템의 안정적인 표시

- [ ] 정산 점수가 `ErosionValue`를 사용하지 않고 선택 ID의 `Score` 합계를 사용하는 실패 테스트를 작성한다.
- [ ] 선택 아이콘 전용 루트와 단계 배지가 없는 현재 상태에서 실패를 확인한다.
- [ ] `ResolveSelectedErosionScore`와 `ResolveErosionIcon`을 로비/전투의 기존 캐시 우선 규칙에 맞게 구현한다.
- [ ] 총점은 붉은 침식 아이콘 옆에 표시하고 선택 항목은 전용 격자에 아이콘과 `I/II/III` 배지로 표시한다.
- [ ] 선택 없음, 잘못된 ID, 캐시 없는 진입을 검증한다.

### Task 2: 캐릭터 행과 상세 정보 오버레이

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ExplorationResultCharacterRowLayoutTests.cs`

**Interfaces:**
- Consumes: `BattleRunCharacterStatisticsData`, 캐릭터 런타임의 장착 유물/스킬 ID, 패널 공용 오버레이 참조
- Produces: `BuildStatisticsTooltip(...)`, 정보 아이콘 전용 호버/클릭, 패널 최상위 공용 오버레이 표시

- [ ] 툴팁 루트가 캐릭터 행의 자식이 아니며 하나의 공용 오버레이로 존재하는 실패 테스트를 작성한다.
- [ ] 현재 씬/스크립트를 대상으로 검사해 실패를 확인한다.
- [ ] 레벨/경험치 바를 초상화 위에 고정하고 유물/스킬 슬롯을 초상화 아래에 재배치한다.
- [ ] 정보 TMP를 실제 아이콘 이미지로 교체하고 포인터 범위를 아이콘 Rect로 제한한다.
- [ ] 상세 통계 텍스트를 정산패널 최상위 공용 오버레이로 옮기고 행에서 통계 문자열과 아이콘 화면 위치만 전달한다.
- [ ] 스킬 아이콘 배율과 간격을 확대한다.
- [ ] 포인터 진입·이탈·클릭 고정 동작과 빈 행 초기화를 검증한다.
- [x] Scene View 피킹 크래시를 재현하는 RectTransform 부모·자식 불일치 검사를 추가하고 모든 정산 패널 참조를 양방향으로 복구한다.
- [x] 세 캐릭터 행의 공통 부모를 아래로 내리고 동일한 수평 기준선을 검증한다.

### Task 3: 획득물 영역과 최종 레이아웃

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ExplorationResultPanelLayoutTests.cs`

**Interfaces:**
- Consumes: `ExplorationResultData.BagItemIds`, `BattleBagItemSlotUI`
- Produces: 독립 획득 아이템 슬롯 루트와 DB 아이콘/수량 표시

- [ ] 획득 아이템 슬롯 루트가 구분선과 분리되고 레퍼런스 배치를 갖는 실패 테스트를 작성한다.
- [ ] 현재 씬을 대상으로 실패를 확인한다.
- [ ] 전용 슬롯 루트를 배치하고 구분선, 제목, 레드더스티움, 확인 버튼 위치를 레퍼런스에 맞춘다.
- [ ] 아이템 슬롯 크기와 간격을 조정하고 최대 슬롯 수 초과 시 안전하게 제한한다.
- [ ] 씬 참조 무결성, 소스 계약, 런타임 및 Editor 어셈블리 컴파일을 검증한다.
- [ ] Unity 에디터 수동 확인이 필요한 항목을 완료 보고에 명시한다.
