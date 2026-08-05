# Next Node Irregular Radial Gradient Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use imagegen for the texture and verification-before-completion before reporting completion. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 다음 노드 선택 영역에 중앙이 짙고 외곽이 불규칙하게 투명해지는 연무형 배경을 적용한다.

**Architecture:** 생성형 이미지로 검은 연무와 균일한 크로마키 외곽을 만든 뒤 알파 PNG로 변환한다. 기존 GradientBackground Image에 새 스프라이트를 연결한다.

**Tech Stack:** Unity 6, UGUI Image, PNG alpha texture, built-in image generation

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 버튼 입력과 맵 상태를 변경하지 않는다.
- Unity batchmode 테스트는 실행하지 않는다.

---

### Task 1: 불규칙 방사형 알파 텍스처

**Files:**
- Create: `Assets/Project/Art/Image/UI/next_node_irregular_radial_gradient.png`
- Create: `Assets/Project/Art/Image/UI/next_node_irregular_radial_gradient.png.meta`

**Interfaces:**
- Produces: 중앙 검정, 외곽 투명인 UI Sprite

- [ ] **Step 1: 크로마키 원본 생성**
  - 균일한 녹색 배경 위에 상하가 뭉툭한 세로형 검은 연무를 생성한다.
- [ ] **Step 2: 투명 PNG 변환**
  - 외곽 크로마키를 알파 0으로 제거하고 가장자리의 부드러운 투명도를 보존한다.
- [ ] **Step 3: 이미지 검증**
  - 모서리 투명, 중앙 불투명, 불규칙한 중간 알파가 존재하는지 확인한다.

### Task 2: Battle 씬 연결

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: 새 UI Sprite GUID
- Produces: `NextNodeSelectionRoot/GradientBackground`의 최종 배경

- [ ] **Step 1: Image 교체**
  - 새 스프라이트를 연결하고 회전 0, 크기 400x800으로 설정한다.
- [ ] **Step 2: 정적 검증**
  - 스프라이트 참조, Raycast 비활성, Ignore Layout, 계층 순서를 확인한다.
