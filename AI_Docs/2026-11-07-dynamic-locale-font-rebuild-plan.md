# Dynamic Locale Font Rebuild Implementation Plan

> **For agentic workers:** 현재 작업 공간에서 계획 순서대로 직접 실행한다. 사용자 승인 없이 커밋, 브랜치 또는 worktree를 만들지 않는다.

**Goal:** 제공된 폰트 파일로 작은 Dynamic TMP Font Asset 구성을 만들고 `LocaleFontDatabase`의 Missing 참조를 복구한다.

**Architecture:** 원본 TTF와 TMP Font Asset을 Source/TMP 폴더로 분리한다. 기존 GUID는 유지하고 DungGeunMo만 정적 아틀라스 데이터를 제거해 Dynamic 에셋으로 재구성한다.

**Tech Stack:** Unity 6, TextMesh Pro, Unity YAML, C#

**Spec:** `AI_Docs/2026-11-07-dynamic-locale-font-rebuild-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- Unity 에디터와 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 하지 않는다.
- 기존 전역 폰트 적용 코드의 동작을 변경하지 않는다.

## Review Focus

- DB GUID와 실제 TMP Font Asset GUID의 일치
- TMP Font Asset과 원본 TTF GUID의 일치
- DungGeunMo 대형 정적 아틀라스 제거
- Dynamic/Multi Atlas 설정의 일관성
- 기존 MASTOD 숫자 폰트 보존 규칙 유지

---

### Task 1: 폰트 에셋 배치

- [ ] Source/TMP 폴더와 메타 파일을 구성한다.
- [ ] 제공된 TTF와 작은 Dynamic SDF 에셋을 복사한다.
- [ ] DungGeunMo Dynamic SDF를 재구성한다.
- [ ] 삭제 전 DungGeunMo GUID를 작은 Dynamic 호환 에셋으로 복구한다.
- [ ] CJK Dynamic SDF를 4096/Multi Atlas로, 숫자 폰트를 1024/Multi Atlas로 정규화한다.

### Task 2: LocaleFontDatabase 연결

- [ ] 언어별 기본 폰트를 실제 에셋 GUID로 연결한다.
- [ ] 공통 숫자 폰트와 소스 역할을 MASTOD로 연결한다.
- [ ] 사용하지 않는 숨은 역할 폰트 참조를 비운다.

### Task 3: 검증

- [ ] GUID, Source Font, Dynamic 설정과 파일 크기를 정적 검사한다.
- [ ] 런타임과 에디터 C# 프로젝트를 컴파일한다.
- [ ] 에디터 화면에서만 확인 가능한 항목을 완료 보고에 구분한다.
