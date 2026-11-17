# Chinese Font Multi Atlas Implementation Plan

**Goal:** 실제 DB가 사용하는 두 중국어 TMP 폰트의 동적 글리프 용량 제한을 제거한다.

**Spec:** `AI_Docs/2026-11-17-chinese-font-multi-atlas-design.md`

## 작업

- [ ] DB 기반 간체·번체 폰트 회귀 테스트를 먼저 작성한다.
- [ ] 현재 두 폰트에서 Multi Atlas 조건이 실패함을 확인한다.
- [ ] 두 FontAsset의 Multi Atlas만 활성화한다.
- [ ] YAML 정적 검사와 런타임/에디터 C# 컴파일을 수행한다.

## 제약

- 문서는 `AI_Docs`에만, 테스트는 `Assets/Tests/EditMode~`에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR, 브랜치 또는 worktree 작업을 수행하지 않는다.

