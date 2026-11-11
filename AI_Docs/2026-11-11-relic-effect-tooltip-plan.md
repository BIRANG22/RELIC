# Relic Effect Tooltip Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 모든 유물 설명의 효과 키워드에 밑줄을 표시하고 호버 시 기존 상태효과 정보 텍스트를 보여준다.

**Architecture:** 지역화된 유물 설명을 공통 포맷터에서 효과 링크로 바꾸고, TMP 전용 공용 프레젠터/호버 컴포넌트가 기존 `UnitStatusEffectTooltipUI`에 연결한다. 각 UI는 공용 프레젠터를 통해 설명을 바인딩한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity EventSystem, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-11-relic-effect-tooltip-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~` 내부에 둔다.
- Unity 에디터는 직접 제어하거나 batchmode로 실행하지 않는다.
- 전투 상태 및 멀티플레이 동기화 경계는 변경하지 않는다.

## Review Focus

- 동일 효과가 여러 번 등장해도 정보 목록에는 한 번만 나타난다.
- 토큰이 없는 일반 설명은 원문을 보존하고 툴팁을 띄우지 않는다.
- 잘못된/없는 효과 ID가 전체 설명 표시를 깨뜨리지 않는다.
- 비활성화·포인터 이탈 시 이 컴포넌트가 연 효과 툴팁만 닫는다.
- 기존 스킬 효과 링크 형식과 호환된다.

---

### Task 1: 유물 설명 공통 포맷

**Files:**
- Create: `Assets/Tests/EditMode~/RelicDescriptionFormatterTests.cs`
- Create: `Assets/Project/Scripts/RelicDescriptionFormatter.cs`
- Modify: `Assets/Project/Art/Image/GameDataLocalization.cs`

**Interfaces:**
- Produces: `RelicDescriptionFormatter.Format(string source)`

- [ ] 효과 토큰의 링크·밑줄 변환, 중복 ID 추출, 일반 텍스트 보존 테스트를 작성한다.
- [ ] 해당 EditMode 테스트가 기능 부재로 실패하는지 확인한다.
- [ ] 포맷터와 지역화 진입점 연결을 최소 구현한다.
- [ ] 대상 테스트와 C# 컴파일을 통과시킨다.

### Task 2: 공용 TMP 호버 표시

**Files:**
- Create: `Assets/Tests/EditMode~/RelicEffectTooltipTextTests.cs`
- Create: `Assets/Project/Scripts/UI/RelicEffectTooltipText.cs`
- Create: `Assets/Project/Scripts/UI/RelicDescriptionPresenter.cs`

**Interfaces:**
- Consumes: `RelicDescriptionFormatter.Format(string source)`
- Produces: `RelicDescriptionPresenter.Set(TMP_Text target, string description)`

- [ ] 바인딩 시 링크 ID와 TMP 컴포넌트 상태가 설정되고 빈 설명은 안전하게 처리되는 테스트를 작성한다.
- [ ] 해당 테스트가 기능 부재로 실패하는지 확인한다.
- [ ] 포인터 진입/이탈과 비활성화 수명주기를 기존 효과 목록 UI에 연결한다.
- [ ] 대상 테스트와 C# 컴파일을 통과시킨다.

### Task 3: 전체 유물 설명 화면 연결

**Files:**
- Modify: 유물 설명을 TMP에 직접 설정하는 전투·보상·이벤트·상점·가방·로비 UI 스크립트
- Test: `Assets/Tests/EditMode~/RelicDescriptionBindingTests.cs`

**Interfaces:**
- Consumes: `RelicDescriptionPresenter.Set(TMP_Text target, string description)`

- [ ] 주요 표시 경로가 공용 프레젠터로 바인딩되는 동작 테스트를 작성한다.
- [ ] 테스트가 기존 직접 대입 때문에 실패하는지 확인한다.
- [ ] 모든 최종 TMP 대입 지점을 공용 프레젠터로 전환한다.
- [ ] 관련 EditMode 테스트와 두 C# 프로젝트 빌드를 실행한다.

### Task 4: 회귀 검증

**Files:**
- Verify only

- [ ] 변경 파일 diff와 유물 설명 호출 경로를 재검색해 누락을 확인한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] Unity 에디터 화면 검증이 필요한 부분을 완료 보고에 명시한다.

