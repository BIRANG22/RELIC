# Reward Data Localization Key Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 획득한 스킬/유물의 ID를 기준으로 이름·효과·레어도가 올바른 로컬라이제이션 키를 사용하도록 수정한다.

**Architecture:** 에디터 스캐너와 런타임이 같은 키 빌더를 사용하게 하고, 보상 패널은 표시할 때마다 ID로 데이터를 재조회한다. 등급처럼 여러 아이템이 공유하는 값은 ID별 키가 아니라 공통 등급 키로 표시한다.

**Tech Stack:** Unity 6, C#, Unity Localization, NUnit EditMode tests

**Spec:** `AI_Docs/2026-09-28-reward-data-localization-key-fix-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 현재 체크아웃과 작업 공간에서 작업하며 커밋·Push·PR은 수행하지 않는다.
- 전투 상태 및 멀티플레이 동기화 데이터는 변경하지 않는다.

## Review Focus

- `SkillMaster`, `MonsterSkill`처럼 camel-case인 시트명이 런타임과 동일하게 정규화되는가.
- 한국어 `효과`와 영문 `Details` 헤더가 모두 `details` 필드로 매핑되는가.
- 알 수 없는 유물 레어도가 빈 문자열이나 잘못된 공통 키로 바뀌지 않는가.
- 저장 복원으로 `Name`이 비어 있거나 이전 언어여도 ID 데이터 조회 결과가 우선되는가.
- 로케일 변경 시 현재 보상 ID로 이름·효과·레어도가 모두 다시 계산되는가.

---

### Task 1: GameData 키 생성 규칙 통일

**Files:**
- Modify: `Assets/Editor/LocalizationProjectScanner.cs`
- Modify: `Assets/Editor/LocalizationManagerWindow.cs`
- Test: `Assets/Tests/EditMode~/RewardDataLocalizationKeyTests.cs`

- [ ] 스캐너 키와 `SkillMaster` 효과 필드에 대한 실패 테스트를 작성한다.
- [ ] 관련 EditMode 테스트를 실행해 기대한 이유로 실패하는지 확인한다.
- [ ] 스캐너가 런타임 키 빌더를 사용하고 시트별 필드 매핑을 지원하도록 수정한다.
- [ ] 관련 테스트를 다시 실행해 통과를 확인한다.

### Task 2: 공통 레어도 및 ID 기반 보상 표시

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Effect/RelicRarityUtility.cs`
- Modify: `Assets/Project/Scripts/BattleRewardEquipPanelUI.cs`
- Test: `Assets/Tests/EditMode~/RewardDataLocalizationKeyTests.cs`

- [ ] 유물 레어도 공통 키 선택에 대한 실패 테스트를 작성한다.
- [ ] 테스트가 새 API 부재로 실패하는지 확인한다.
- [ ] 유물 레어도 표시 API와 보상 패널의 ID 기반 재조회를 구현한다.
- [ ] 관련 테스트와 C# 컴파일을 검증한다.

### Task 3: 로컬라이제이션 데이터 동기화 및 전체 검증

**Files:**
- Modify through Unity Localization Manager: `Assets/ExcelSource/Localization.xlsx`
- Modify through Unity Localization importer: `Assets/Language/Text Shared Data.asset`, locale tables

- [ ] Localization Manager 전체 검사 및 적용으로 신규 키를 생성한다.
- [ ] 잘못된 구형 키가 제거되고 신규 키가 locale table에 존재하는지 확인한다.
- [ ] 관련 EditMode 테스트와 양쪽 C# 어셈블리 컴파일을 검증한다.
- [ ] Unity 에디터에서 실제 언어 전환 표시가 필요한 항목은 수동 검증 항목으로 기록한다.
# 후속 수정 계획

1. 네 동적 텍스트 모두 정적 로컬라이저의 관리 대상에서 제외한다.
2. `Cost_Value`를 패널의 동적 텍스트 보호 목록에 추가한다.
3. 네 필드가 동일한 보호 계약을 따르는 회귀 테스트를 추가한다.
4. 스크립트 컴파일과 씬 직렬화 상태를 검증한다.

## 원본 프리팹 및 번역 테이블 후속 작업

1. 원본 `Equip_panel.prefab`의 Name/Rarity/Effect를 동적 출력으로 변경한다.
2. 레거시 `data.skillmaster.*` 이름 키를 canonical 키로 마이그레이션한다.
3. 모든 SkillMaster ID의 `name`, `details` 행을 워크북에 생성한다.
4. 프리팹 소유권과 워크북 커버리지 회귀 테스트를 검증한다.

## 전역 자동 바인딩 덮어쓰기 후속 작업

1. presenter가 최종 번역값을 쓰는 TMP를 전역 자동 바인딩에서 제외하는 표식을 추가한다.
2. Equip_panel의 Name/Rarity/Effect/Cost_Value 보호 시 표식을 적용하고 기존 동적 작성자를 비활성화한다.
3. 전역 자동 작성자가 표식이 있는 TMP를 다시 갱신하지 않도록 차단한다.
4. 정적·동적 작성자 모두 차단되는 회귀 테스트를 검증한다.

## 보상 아이콘 및 레어도 fallback 후속 작업

1. 비어 있는 번역 원본은 건드리지 않고 관련 커버리지 테스트도 한국어 원문이 있는 행만 요구하도록 조정한다.
2. 스킬·유물 보상 아이콘을 `RewardId` 기반 DB에서 재조회하고 캐시 아이콘은 최종 fallback으로만 사용한다.
3. 스킬 레어도에 명시적인 한국어 fallback을 제공한다.
4. ID 기반 아이콘 복원, 한국어 레어도 fallback, 런타임/Editor 컴파일을 검증한다.
