# Lobby Relic VFX Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 유물상점의 `magic_ring_06` VFX를 유물 아이콘 뒤에 렌더링한다.

**Architecture:** VFX 정렬은 유지하고 세 `RelicIcon`에 독립 정렬 Canvas를 부여한다. 아이콘 Canvas를 같은 `Unit` Sorting Layer의 더 높은 Order로 표시해 카드 배경, VFX, 아이콘 순서를 보장한다.

**Tech Stack:** Unity 6, Canvas, ParticleSystemRenderer, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- 원본 `magic_ring_06` 프리팹은 변경하지 않는다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR을 수행하지 않는다.

---

### Task 1: 유물 아이콘 정렬 Canvas 추가

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Test: `Assets/Tests/EditMode~/LobbyRelicOfferButtonUITests.cs`

**Interfaces:**
- Consumes: `RelicOffer_1~3/RelicIcon`, Sorting Layer `Unit`
- Produces: `overrideSorting = true`, `sortingOrder = 10`인 아이콘 Canvas 3개

- [ ] **Step 1: 실패 회귀 테스트 작성**

  Lobby 씬을 Preview Scene으로 열어 세 유물 아이콘의 Canvas와 정렬값을 검사한다.

- [ ] **Step 2: 구현 전 실패 확인**

  현재 세 아이콘에 Canvas가 없어 검사에 실패하는지 확인한다.

- [ ] **Step 3: 최소 씬 구현**

  각 `RelicIcon`에 중첩 Canvas를 추가하고 `Unit`, Order 10으로 설정한다.

- [ ] **Step 4: 검증**

  씬 YAML 검사, `Assembly-CSharp`, `Assembly-CSharp-Editor` 빌드를 실행한다.

