# Checkpoint Auto Save Implementation Plan

**Goal:** 전투 진행 저장을 확정된 체크포인트에만 기록하고, Continue·Abandon·Save & Exit가 마지막 체크포인트를 정확히 유지하도록 한다.

**Architecture:** `SaveSystem`에 공용 `SaveCheckpoint` API와 Continue 복원 억제를 둔다. BattleEntry, BattleReward, MapSelection, Event, Rest는 자신의 상태가 확정된 수명주기에서만 이를 호출한다. Save & Exit와 Abandon은 새 런타임 스냅샷을 만들지 않는다.

**Tech Stack:** Unity C#, NUnit EditMode tests.

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- `SaveCurrentProgress`는 로비/영구 데이터 저장 역할을 유지한다.
- Battle 저장은 `Command -> State Change -> Result/Event` 결과가 확정된 뒤에만 기록한다.
- Continue 복원 중에는 저장 파일과 `SavedAtUtc`를 갱신하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

### Task 1: Checkpoint 저장 경계와 회귀 테스트

- [ ] 저장 API와 호출 지점을 검증하는 EditMode 테스트를 먼저 추가한다.
- [ ] 테스트가 기존 구현에서 실패함을 확인한다.
- [ ] `SaveSystem.SaveCheckpoint`와 복원 억제 API를 최소 구현한다.
- [ ] 테스트를 다시 실행한다.

### Task 2: BattleEntry 및 BattleReward

- [ ] BattleEntry의 Player/Monster/Grid/GridEffect/초기 명령 확정 직후 checkpoint를 저장한다.
- [ ] Reward가 실제 확정된 시점에 BattleEntry checkpoint를 덮어쓴다.
- [ ] 턴 진행 중 저장이 발생하지 않는지 검증한다.

### Task 3: MapSelection, Event, Rest

- [ ] 노드 완료와 RuntimeData 반영 뒤 MapSelection checkpoint를 저장하고 transient 상태를 제거한다.
- [ ] 실제 Dice 단계만 Event checkpoint를 저장하도록 제한한다.
- [ ] Rest는 조사된 의미 있는 확정 단계만 저장한다.

### Task 4: Save & Exit, Abandon, Continue

- [ ] Save & Exit에서 모든 진행 저장 호출을 제거한다.
- [ ] Abandon은 checkpoint 파일만 삭제하고 새 저장을 만들지 않게 한다.
- [ ] Continue 적용/씬 전환 중 autosave가 억제되는지 검증한다.

### Task 5: 검증

- [ ] 관련 EditMode 테스트와 컴파일을 실행한다.
- [ ] `git diff --check` 및 파일 단위 diff를 검토한다.
