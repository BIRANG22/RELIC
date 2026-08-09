# Player-facing Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Title`, `Lobby`, `Battle`의 플레이어 노출 텍스트를 다섯 Locale 번역표와 실제 Localization 참조에 연결한다.

**Architecture:** Editor 감사/마이그레이션 도구가 정적 TMP 문구를 수집하고 `LocalizeStringEvent`로 연결한다. 동적 문구는 `GameLocalization`으로 조회하며 `Localization.xlsx`가 번역의 단일 원본이다.

**Tech Stack:** Unity 6, Unity Localization 1.5.11, TextMesh Pro, C# Editor API, NUnit EditMode, XLSX

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치와 worktree 작업은 수행하지 않는다.
- 전투 결과와 멀티플레이 동기화 데이터는 변경하지 않는다.

---

### Task 1: 플레이어 문구 인벤토리와 Key 매핑

- [ ] 세 씬과 UI 프리팹의 TMP 텍스트 후보를 추출한다.
- [ ] 디버그·숫자·런타임 자리표시자를 제외한다.
- [ ] 중복 문구를 문맥별 Key로 정규화한다.
- [ ] 인벤토리와 Key 매핑의 회귀 테스트를 작성한다.

### Task 2: 번역 워크북 확장

- [ ] 현재 `Localization.xlsx`의 사용자 편집 내용을 보존해 읽는다.
- [ ] Key와 다섯 언어 번역을 Merge한다.
- [ ] 빈 번역, 중복 Key와 필드 수를 검사한다.

### Task 3: 정적 TMP 텍스트 연결

- [ ] 반복 실행 가능한 Editor 마이그레이션 도구 테스트를 작성한다.
- [ ] `Title`, `Lobby`, `Battle` 및 대상 프리팹에 `LocalizeStringEvent`를 연결한다.
- [ ] 이미 연결된 텍스트와 런타임 전용 텍스트를 보존한다.

### Task 4: 동적 플레이어 문구 연결

- [ ] `GameLocalization` 조회와 인자 처리 테스트를 작성한다.
- [ ] 확인창, 토스트, 경고와 상태 설명의 하드코딩 문구를 Key 조회로 교체한다.
- [ ] UI가 전투 상태를 계산하거나 변경하지 않는지 검토한다.

### Task 5: 누락 감사와 검증

- [ ] 대상 범위의 미연결 텍스트 감사 결과를 생성한다.
- [ ] 신규 테스트 포함 Editor 어셈블리를 컴파일한다.
- [ ] 워크북 구조와 번역 누락을 검사한다.
- [ ] Unity에서 수동 확인할 레이아웃·폰트 항목을 정리한다.
