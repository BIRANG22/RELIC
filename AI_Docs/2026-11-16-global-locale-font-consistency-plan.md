# 전역 언어 폰트 일관성 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 언어별 폰트 Database의 선택 결과를 일반 TMP 전체에 일관되게 적용하여 기존 직렬화 폰트와 섞이지 않게 한다.

**Architecture:** `GlobalLocalizedTMPFontApplicator`가 모든 일반 TMP의 기본 폰트 소유자가 된다. 명시적 역할, 다국어 드롭다운, Blur 복제 및 Database의 보존 매핑만 예외로 유지한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity Localization, NUnit EditMode

**Spec:** `AI_Docs/2026-11-16-global-locale-font-consistency-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트와 Unity 에디터 직접 제어를 수행하지 않는다.
- 씬·프리팹 YAML을 대량 치환하지 않는다.
- 커밋, Push, PR을 수행하지 않는다.

## Review Focus

- Localization 컴포넌트가 없는 라틴 문자열도 현재 Locale 본문 폰트를 사용해야 한다.
- 숫자·퍼센트·`LV.n`은 공통 숫자 폰트를 사용해야 한다.
- `preserveSourceFont`가 설정된 특수 폰트는 변경되지 않아야 한다.
- 명시적 `LocalizedTMPFont` 역할은 전역 기본 역할보다 우선해야 한다.
- Locale 또는 DB 폰트 누락 시 기존 폰트를 손상하지 않아야 한다.

---

### Task 1: 전역 적용 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

**Interfaces:**
- Consumes: `GlobalLocalizedTMPFontApplicator.ApplyToText(TMP_Text, LocaleFontDatabase, Locale)`
- Produces: 일반 라틴 TMP 및 숫자 TMP의 기대 동작

- [ ] Localization 컴포넌트 없는 라틴 TMP가 본문 폰트로 변경되는 실패 테스트를 작성한다.
- [ ] 숫자 TMP가 공통 숫자 폰트로 변경되는 실패 테스트를 작성한다.
- [ ] 현재 구현에서 기대 분기가 충족되지 않음을 확인한다.

### Task 2: 전역 역할 결정 단순화

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs`

**Interfaces:**
- Consumes: `LocaleFontDatabase.TryGetRoleIdForSourceFont`, `LocaleFontDatabase.TryResolve`
- Produces: 모든 일반 TMP에 대한 결정적 역할 및 폰트 적용

- [ ] 문자열 문자권 및 Localization 컴포넌트 기반 적용 필터를 제거한다.
- [ ] 숫자 중심 문자열은 `number`, 그 외 일반 문자열은 매핑된 역할 또는 `body`를 선택한다.
- [ ] `preserveSourceFont`, 드롭다운, Blur, 명시적 역할 소유권을 유지한다.
- [ ] 테스트 코드와 런타임 코드를 컴파일한다.

### Task 3: 정적·컴파일 검증

**Files:**
- Verify: `Assets/Project/Scripts/Core/Localization/GlobalLocalizedTMPFontApplicator.cs`
- Verify: `Assets/Tests/EditMode~/LocaleFontDatabaseTests.cs`

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: 완료 보고용 검증 근거

- [ ] `Assembly-CSharp.csproj`를 빌드한다.
- [ ] `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] Git diff에서 승인 범위 밖 변경이 없는지 확인한다.
- [ ] Unity 화면 검증과 EditMode 실행은 미검증 항목으로 기록한다.
