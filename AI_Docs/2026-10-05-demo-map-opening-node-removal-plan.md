# Demo Map Opening Node Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 데모 맵의 첫 고정 맵을 제거하고 나머지 경로를 한 레이어씩 앞으로 이동한다.

**Architecture:** 기존 `ManualBattleMapTemplate`의 예약 노드 인덱스 규칙을 유지하고 ScriptableObject 직렬화 데이터만 재배치한다. 에디트 모드 테스트는 생성된 `Nodes` 결과를 검증해 데이터 구조와 연결 회귀를 방지한다.

**Tech Stack:** Unity 6, C#, NUnit, Unity ScriptableObject YAML

**Spec:** `AI_Docs/2026-10-05-demo-map-opening-node-removal-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에 둔다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 제거 대상 `Map_25`가 시작 노드에 남지 않는지 확인한다.
- 당겨진 노드의 타입과 맵 오버라이드가 유지되는지 확인한다.
- 예약 노드 인덱스 연결이 다음 활성 노드를 정확히 가리키는지 확인한다.
- 보스 레이어가 5로 이동하는지 확인한다.
- 모든 활성 노드가 가운데 한 줄 위치를 유지하는지 확인한다.

---

### Task 1: 데모 맵 경로 회귀 테스트 및 데이터 재배치

**Files:**
- Modify: `Assets/Tests/EditMode~/ManualBattleMapTemplateTests.cs`
- Modify: `Assets/Project/Data/MapTemplates/Test Manual Battle Map Template Demo.asset`

**Interfaces:**
- Consumes: `ManualBattleMapTemplate.Nodes`
- Produces: 레이어 0~5의 단일 데모 경로

- [ ] **Step 1: 실패하는 회귀 테스트 작성**

  노드 타입, `Map_25` 제거, 예약 인덱스 연결, 위치를 리터럴 기대값으로 검증한다.

- [ ] **Step 2: 변경 전 실패 확인**

  Unity batchmode 대신 현재 에셋을 읽는 정적 검증으로 기존 경로가 새 기대값과 다름을 확인한다.

- [ ] **Step 3: 맵 템플릿 데이터 재배치**

  `bossLayerIndex`를 5로 바꾸고 레이어 1~5의 활성 노드를 한 칸씩 앞으로 옮긴다.

- [ ] **Step 4: 변경 후 검증**

  에셋 정적 검증, C# 프로젝트 컴파일, 변경 diff 검토를 수행한다.
