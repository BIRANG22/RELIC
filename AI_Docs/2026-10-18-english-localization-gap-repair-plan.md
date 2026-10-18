# 영어 로컬라이징 누락 및 우회 경로 보완 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 영어 로케일에서 스킬 이름·설명과 추출 버튼을 포함한 누락 또는 한국어 노출을 제거한다.

**Architecture:** `Localization.xlsx`를 번역 단일 원본으로 유지하고 Unity String Table은 가져오기 결과물로 취급한다. 동적 스킬 UI는 안정적인 SkillId 기반 `GameDataLocalization` 경로만 사용하고, 누락 번역 표기는 선택 언어의 정책을 유지한다.

**Tech Stack:** Unity 6, C#, Unity Localization, XLSX

**Spec:** `AI_Docs/2026-10-18-english-localization-gap-repair-design.md`

## Global Constraints

- 현재 작업 공간과 브랜치에서 작업한다.
- 문서는 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~` 아래에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
- 기존 영어 번역과 사용자의 현재 변경을 보존한다.

## Review Focus

- 영어 누락 표기가 한국어 원문으로 다시 바뀌지 않아야 한다.
- 토큰 전용 스킬 설명도 번역 대상으로 남고 모든 토큰이 보존되어야 한다.
- 동적 UI 작성자와 정적 로컬라이저가 동일 TMP를 경쟁해서 갱신하지 않아야 한다.
- 신규 워크북 행이 Unity String Table에서 누락되지 않아야 한다.
- 기존 번역과 다른 언어 열이 번역 보완 과정에서 변경되지 않아야 한다.

---

### Task 1: 회귀 테스트 추가

**Files:**
- Modify: `Assets/Tests/EditMode~/LocalizationComprehensiveCoverageTests.cs`
- Modify: `Assets/Tests/EditMode~/MonsterInfoNameAndPassiveActivationTests.cs`

- [ ] 영어 공란, 추출 버튼, 활성 스킬 번역, 토큰 일치 검사를 추가한다.
- [ ] 비한국어 로케일의 누락 표기가 한국어 원문으로 대체되지 않는 테스트를 추가한다.
- [ ] 테스트를 실행해 현재 구현에서 의도한 실패를 확인한다.

### Task 2: 번역 데이터 보완 및 동기화

**Files:**
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_en.asset`
- Preserve/update importer-affected locale assets only as required

- [ ] 영어가 비어 있고 한국어 원문이 있는 238개 셀을 번역한다.
- [ ] 워크북 구조와 기존 값을 보존했는지 검사한다.
- [ ] Unity Localization Import로 String Table을 갱신한다.

### Task 3: 스킬 표시 우회 경로 수정

**Files:**
- Modify: `Assets/Project/Art/Image/GameDataLocalization.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillTooltipFormatter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs`
- Modify other confirmed player-facing raw skill field consumers only when required by the regression tests

- [ ] 선택 로케일 누락 표기의 한국어 재대체를 제거한다.
- [ ] 스킬 설명 원본을 `GameDataLocalization.SkillDetailsTemplate`로 통일한다.
- [ ] 확인된 스킬 이름 직접 표시를 `GameDataLocalization.SkillName`으로 교체한다.
- [ ] 회귀 테스트가 통과하는지 확인한다.

### Task 4: 최종 검증

- [ ] 워크북 영어 공란, 기존 영어 보존, 토큰 일치 및 키 중복을 검사한다.
- [ ] 관련 EditMode 테스트를 Unity Test Runner에서 실행한다.
- [ ] `Assembly-CSharp`와 `Assembly-CSharp-Editor`를 컴파일한다.
- [ ] 변경 범위와 기존 작업 보존 여부를 diff로 검토한다.

### Task 5: 동적 효과 문장 키 보완

**Files:**
- Modify: `Assets/Editor/LocalizationProjectScanner.cs`
- Modify: `Assets/Editor/LocalizationManagerWindow.cs`
- Modify: `Assets/Editor/LocalizationSyncCommand.cs`
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text_en.asset`
- Modify: `Assets/Tests/EditMode~/LocalizationComprehensiveCoverageTests.cs`

- [ ] 현재 SkillMaster 데이터가 요구하는 동적 효과 문장 키를 계산하는 회귀 테스트를 추가한다.
- [ ] 테스트가 런타임 카탈로그/엑셀 키 누락으로 실패하는 것을 확인한다.
- [ ] 문장 템플릿을 런타임 소스 카탈로그에 등록하고 전체 검사·정리·동기화 경로에 연결한다.
- [ ] 엑셀과 영어 String Table에 한국어·영어 템플릿을 추가한다.
- [ ] 데이터 검사와 런타임/에디터 어셈블리 컴파일로 검증한다.
