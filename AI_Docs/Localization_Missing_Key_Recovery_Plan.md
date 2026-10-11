# Localization Missing Key Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전체 검사에서 빠지는 로컬라이제이션 키를 생성하고 런타임 한국어 하드코딩과 잘못된 EffectId를 제거한다.

**Architecture:** 데이터별 수집 정책은 `LocalizationProjectScanner`에 두고, 동적 튜토리얼 원문은 에디터 소스 카탈로그로 중앙화한다. 런타임은 키만 소비하며 엑셀 원본과 생성 데이터의 정합성을 회귀 테스트로 보장한다.

**Tech Stack:** Unity C#, NUnit EditMode, XLSX 로컬라이제이션/게임 데이터

**Spec:** `AI_Docs/Localization_Missing_Key_Recovery_Design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 커밋, Push, PR, 브랜치/worktree 작업은 수행하지 않는다.

## Review Focus

- 토큰만 있는 Details와 `-` placeholder를 구분한다.
- 기존 한글 기반 게임 데이터 스캔 동작을 보존한다.
- 동적 튜토리얼 키의 중복 등록을 만들지 않는다.
- 워크북 형식과 기존 번역을 보존한다.
- EffectId 원본과 런타임 CSV를 함께 맞춘다.

---

### Task 1: 회귀 테스트

- [ ] 토큰 전용 Details 수집 정책 테스트를 추가한다.
- [ ] 튜토리얼 키/영어/런타임 하드코딩 부재 테스트를 추가한다.
- [ ] 잘못된 EffectId 부재 테스트를 추가하고 실패를 확인한다.

### Task 2: 스캐너와 튜토리얼 소스 개선

- [ ] `ShouldCollectGameDataLocalizationValue`를 추가해 스캔과 참조 키 수집에 공통 적용한다.
- [ ] 첫 전투 튜토리얼 소스 카탈로그를 추가하고 전체 검사/동기화에서 사용한다.
- [ ] 튜토리얼 컨트롤러의 fallback 매개변수·상태·한국어 리터럴을 제거한다.

### Task 3: 데이터 복구

- [ ] 누락된 Skill Details 및 Tutorial 키를 `Localization.xlsx`에 추가한다.
- [ ] `GameData.xlsx`의 EffectId 오타를 수정하고 런타임 CSV에 반영한다.

### Task 4: 검증

- [ ] C# 컴파일 및 EditMode 프로젝트 테스트 소스 검사를 실행한다.
- [ ] 워크북 키/번역/EffectId 정합성을 독립 검사한다.
- [ ] 변경 범위와 미검증 항목을 보고한다.
