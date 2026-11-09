# Title Expedition Message Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 타이틀 탐사 포기 완료 및 탐사 데이터 부재 안내를 6개 지원 언어로 정상 표시한다.

**Architecture:** 권위 원본인 `Localization.xlsx`에 두 행을 추가하고 Unity Localization 공유 키와 로케일 asset에 동일 키/문구를 반영한다. 기존 종합 커버리지 테스트의 필수 키 목록으로 재발을 방지한다.

**Tech Stack:** Unity Localization, XLSX, NUnit EditMode

**Spec:** `AI_Docs/2026-11-09-title-expedition-message-localization-design.md`

## Global Constraints

- 기존 워크북 열·서식·행을 보존하고 두 번역 행만 추가한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에서만 수정한다.
- Unity batchmode는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 두 키 모두 원본 워크북에 존재하고 한국어·영어가 비어 있지 않아야 한다.
- 공유 키 ID와 6개 로케일 asset의 ID가 일치해야 한다.
- 기존 번역 행과 워크북 서식이 변경되지 않아야 한다.
- 확인창 기존 번역은 변경되지 않아야 한다.
- 런타임 호출 키와 추가한 키 철자가 정확히 일치해야 한다.

---

### Task 1: 번역 누락 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/LocalizationComprehensiveCoverageTests.cs`

**Interfaces:**
- Produces: 두 시스템 키를 요구하는 `RequiredRuntimeKeys`

- [ ] 두 키를 필수 키 목록에 추가한다.
- [ ] 현재 워크북을 검사해 두 키가 없어서 실패함을 확인한다.

### Task 2: 워크북 및 Unity 테이블 동기화

**Files:**
- Modify: `Assets/ExcelSource/Localization.xlsx`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_ko.asset`
- Modify: `Assets/Language/Text_en.asset`
- Modify: `Assets/Language/Text_ja.asset`
- Modify: `Assets/Language/Text_zh-Hans.asset`
- Modify: `Assets/Language/Text_zh-Hant.asset`
- Modify: `Assets/Language/Text_es.asset`

**Interfaces:**
- Consumes: 설계 문서의 두 키와 6개 언어 문구
- Produces: 원본과 런타임 테이블의 일치된 번역

- [ ] 기존 워크북 형식과 마지막 행을 검사한다.
- [ ] 두 행을 기존 스타일로 추가하고 재계산·렌더·내보내기 검증한다.
- [ ] 충돌하지 않는 공유 ID 두 개를 추가하고 모든 로케일 asset에 동일 ID로 번역을 추가한다.
- [ ] 워크북과 asset의 키·문구·ID 일치를 검사한다.

### Task 3: 최종 검증

**Files:**
- Verify only

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: 컴파일 및 데이터 무결성 근거

- [ ] 런타임·에디터 C# 어셈블리를 빌드한다.
- [ ] 관련 워크북 키와 6개 로케일 값을 독립적으로 검사한다.
- [ ] diff와 독립 리뷰로 요청 범위 밖 변경이 없는지 확인한다.
