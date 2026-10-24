# UI Blur Replica Resolution Scale Implementation Plan

> **For agentic workers:** Implement inline in the current checkout. Do not create commits, branches, PRs, or worktrees without user approval.

**Goal:** 해상도별 Canvas 배율이 블러 복제 루트에 중복 적용되지 않게 한다.

**Architecture:** `UIBlurReplicaSource.TransformPair`의 루트 동기화는 화면 모서리로 위치와 크기를 완전히 재구성하므로 단위 스케일을 사용한다. 자식 동기화는 로컬 레이아웃 복제이므로 원본 스케일을 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-24-ui-blur-replica-resolution-scale-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 멀티플레이 및 전투 핵심 상태에는 영향을 주지 않는다.

## Review Focus

- 비단위 루트 스케일이 복제 루트에 재적용되지 않는가.
- 자식의 비단위 로컬 스케일은 그대로 유지되는가.
- 루트 위치와 크기를 계산하는 기존 화면 좌표 변환은 유지되는가.

### Task 1: 블러 복제 루트 스케일 회귀 수정

**Files:**
- Modify: `Assets/Project/Scripts/UIBlurReplicaSource.cs`
- Create: `Assets/Tests/EditMode~/UIBlurReplicaSourceScaleTests.cs`

- [ ] 루트 복제본은 단위 스케일이고 자식 복제본은 원본 로컬 스케일을 유지해야 한다는 실패 테스트를 작성한다.
- [ ] 테스트가 기존 구현의 루트 스케일 중복 적용으로 실패하는지 확인한다.
- [ ] 루트 동기화에서만 `Vector3.one`을 적용한다.
- [ ] 관련 테스트와 C# 컴파일을 실행한다.

