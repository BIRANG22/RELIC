# SDD ledger — plan: AI_Docs/2026-10-19-battle-room-party-transition-plan.md

- Ruling: 프로젝트 AGENTS.md가 현재 작업 공간 사용과 사용자 승인 없는 worktree·commit을 요구하므로 isolated worktree와 task commit 단계를 생략한다. 비용: 작업별 Git commit 경계가 없으며 최종 diff로 전체 검토한다.
- Pre-flight: Task 1의 좌표 API를 Task 2가 사용하고 시그니처가 일치한다.
- Pre-flight: Task 2의 Presenter API를 Task 3·4가 사용하고 시그니처가 일치한다.
- Pre-flight: Task 3의 Loader 진입 흐름과 Task 4의 SceneController 퇴장 흐름은 Presenter 상태를 공유하며, Presenter가 중복·취소를 직렬화한다.
- Ruling: 프로젝트 규칙상 batchmode 테스트를 실행하지 않으므로 RED/GREEN은 테스트 소스 포함 MSBuild 컴파일과 순수 API 계약 검증으로 수행한다. 비용: Unity Test Runner assertion 실행은 에디터 UI 접근이 없으면 미검증으로 남는다.
- Task 4 Ruling: 기존 화면 전환 API는 가림 콜백 내부 비동기 대기를 지원하지 않는다. 공개 API 변경 없이 캐릭터 퇴장 Task와 기존 전체 화면 전환 Task를 `Task.WhenAll`로 동시에 실행한다. 비용: 방 정리 콜백은 기존 cover 완료 시점을 유지하므로 퇴장 duration은 cover duration 이하로 설정해야 한다.
- Task 1: complete (tests: 좌표 API 구현 전 CS0103 실패 확인 → 구현 후 전체 테스트 어셈블리 컴파일 성공).
- Task 2: complete (tests: Presenter 타입 구현 전 CS0246 실패 확인 → 구현 후 Presenter 계약 포함 컴파일 성공).
- Task 3: complete (Loader가 스폰 직후 진입 연출을 시작하고 입력 활성화 전에 Presenter 완료를 기다림).
- Task 4: complete (전투방에 한해 기존 RoomToMap 화면 전환과 퇴장 Task를 동시에 실행).
- Task 5: complete (BattleRoom에 Presenter 배치, Loader 명시 참조, entry -X/exit +X 및 exit 0.3초 직렬화).
- Review fixes: 0초 duration 동기 완료 시 Task/IsPlaying 상태를 안전하게 유지한다.
- Review fixes: 화면 가림 유지 0.3초 뒤 진입 이동을 시작해 uncover 구간에서 연출이 보이게 한다.
- Review fixes: 캡처 후 Animator가 파괴돼도 Unity fake-null 검사로 안전하게 종료한다.
- Final verification: 런타임과 EditMode 테스트 소스 포함 컴파일, 일반 Assembly-CSharp-Editor 빌드, `git diff --check`, 씬 참조 정적 검증 통과.
- Unverified: 프로젝트 규칙에 따라 Unity batchmode 테스트는 실행하지 않았다.
- Crash fix: Battle 씬의 기존 EventRoom 컴포넌트와 신규 Presenter가 같은 local object ID `920000000000000004`를 사용해 역직렬화가 실패했다. EventRoom ID는 유지하고 Presenter 및 참조만 `920000000000000005`로 변경했다.
- Crash regression: Battle 씬 YAML의 모든 object declaration ID가 고유한지 검사하는 EditMode 테스트를 추가했다.
