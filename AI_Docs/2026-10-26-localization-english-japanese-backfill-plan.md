# 로컬라이징 영어·일본어 공란 보완 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Localization.xlsx`의 한국어 원문이 있는 영어·일본어 공란을 기존 데이터와 형식을 보존하면서 모두 번역한다.

**Architecture:** 워크북을 단일 번역 원본으로 유지한다. 기존 번역 사전과 Key 문맥을 이용해 빈 셀만 채우고, 변경 전 스냅샷과 토큰 검사를 통해 범위를 검증한다.

**Tech Stack:** XLSX, `@oai/artifact-tool`, bundled Python/openpyxl read-only verification

**Spec:** `AI_Docs/2026-10-26-localization-english-japanese-backfill-design.md`

## Global Constraints

- 현재 작업 공간과 브랜치에서 작업한다.
- 문서는 `AI_Docs` 안에만 작성한다.
- 기존 번역과 다른 언어 열을 보존한다.
- Unity 에디터를 직접 제어하거나 batchmode를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 토큰 또는 리치 텍스트 태그가 번역 과정에서 누락·변형되지 않아야 한다.
- 기존 영어·일본어 번역은 한 셀도 변경되지 않아야 한다.
- 줄바꿈과 숫자 조건이 의미를 유지해야 한다.
- 같은 게임 용어가 기존 번역과 일관되어야 한다.
- 워크북 구조·서식·행 순서가 보존되어야 한다.

---

### Task 1: 번역 소스와 재사용 데이터 준비

**Files:**
- Read: `Assets/ExcelSource/Localization.xlsx`
- Create: 실행 작업 공간의 임시 번역 매핑 파일

- [ ] 기존 Unity 언어 자산과 워크북에서 재사용할 영어·일본어 번역을 수집한다.
- [ ] 영어 147개와 일본어 1,382개의 대상 Key를 확정한다.
- [ ] 토큰·태그·줄바꿈 메타데이터를 기록한다.

### Task 2: 공란 번역 및 워크북 저장

**Files:**
- Modify: `Assets/ExcelSource/Localization.xlsx`

- [ ] `mark_artifact_operation_started.mjs`를 edit 작업으로 한 번 실행한다.
- [ ] 기존 번역과 용어를 재사용하고 남은 항목을 문맥에 맞게 번역한다.
- [ ] 영어와 일본어의 빈 셀만 수정한다.
- [ ] 재계산 후 원본 경로에 XLSX를 저장한다.

### Task 3: 데이터 및 시각 검증

**Files:**
- Verify: `Assets/ExcelSource/Localization.xlsx`

- [ ] 영어·일본어 공란 수가 0인지 검사한다.
- [ ] 기존 셀과 대상 외 열이 변경되지 않았는지 비교한다.
- [ ] 토큰·태그 보존, Key 중복, 수식 오류, 시트 구조를 검사한다.
- [ ] 변경 범위를 렌더링하여 잘림이나 구조 손상이 없는지 확인한다.
- [ ] 최종 파일을 다시 열어 검사 결과를 확정한다.
