# Next Node Selection Gradient Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:verification-before-completion to verify this scene-only plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 다음 노드 선택 영역에 왼쪽 투명→오른쪽 검정 그라데이션 배경을 추가한다.

**Architecture:** 기존 알파 그라데이션 스프라이트를 회전한 독립 배경 Image로 재사용한다. LayoutElement로 런타임 VerticalLayoutGroup에서 제외한다.

**Tech Stack:** Unity 6, UGUI Image, LayoutElement

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 버튼 입력 및 맵 상태를 변경하지 않는다.

---

### Task 1: GradientBackground 씬 구성

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: `gradaion2.png` sprite GUID `e2be6e65d1a06b4459477a450a0f4e1c`
- Produces: `NextNodeSelectionRoot/GradientBackground`

- [ ] **Step 1: 배경 오브젝트 추가**
  - RectTransform, CanvasRenderer, Image, LayoutElement를 가진 첫 번째 자식을 추가한다.
- [ ] **Step 2: 방향과 입력 설정**
  - 크기 720x450, 회전 +90도, Image Type Simple, Raycast Target false, Ignore Layout true로 설정한다.
- [ ] **Step 3: 정적 검증**
  - 부모 참조, sprite GUID, 컴포넌트 수, 회전, Raycast 및 Ignore Layout 값을 확인한다.
