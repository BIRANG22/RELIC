# Character HUD First-frame Position Implementation Plan

> **For agentic workers:** Implement directly in the current workspace using test-first changes and fresh compile verification.

**Goal:** 캐릭터 HUD가 유효한 캐릭터 머리 위 위치를 확보하기 전에는 화면에 표시되지 않게 한다.

**Architecture:** `CharacterHUDSlot`이 표시 요청과 실제 렌더링 가능 상태를 분리한다. 위치 계산 성공을 표시 활성화의 선행 조건으로 만들고 실패 시 `LateUpdate`에서 재시도한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-15-character-hud-first-frame-position-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode를 사용하지 않는다.
- UI는 전투 결과를 계산하지 않는다.

## Review Focus

- 추적 타깃이 없을 때 `Show()`가 HUD를 노출하지 않아야 한다.
- 유효한 위치가 계산되면 같은 프레임에 위치 적용 후 노출되어야 한다.
- `Hide()` 이후 지연 표시 요청이 남지 않아야 한다.
- 기존 HP·실드 표시 애니메이션은 표시 완료 후 정상 시작해야 한다.

### Task 1: 위치 확정 후 HUD 표시

**Files:**
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`
- Modify: `Assets/Project/Scripts/CharacterHUDSlot.cs`

- [ ] 추적 위치가 없는 `Show()` 호출이 HUD를 숨김 상태로 유지하는 실패 테스트를 작성한다.
- [ ] 현재 구현에서 테스트가 실패하는지 확인한다.
- [ ] 위치 갱신 성공 여부와 지연 표시 상태를 추가한다.
- [ ] 생성 직후 숨김, 위치 성공 후 표시, `Hide()` 시 요청 해제를 구현한다.
- [ ] 런타임·에디터 컴파일과 diff 검사를 실행한다.

