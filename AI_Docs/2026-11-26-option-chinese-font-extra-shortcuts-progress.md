# 실행 기록 — AI_Docs/2026-11-26-option-chinese-font-extra-shortcuts-plan.md

- Ruling: 프로젝트 규칙에 따라 현재 작업 공간을 사용하며 커밋/worktree를 만들지 않는다.
- Ruling: EditMode~ 테스트는 작성하지만 생성된 csproj에서 제외되고 batchmode가 금지되어 RED/GREEN 실행은 할 수 없다. 컴파일과 정적 호출 경로 검사로 보완한다.
- Pre-flight: 바인딩 모델은 옵션 UI, 전투 입력, 튜토리얼, 키 표기의 공통 인터페이스다. 모든 소비자가 같은 `BattleShortcutAction` 값을 사용한다.
- Review fix: 캐릭터 숫자 선택 입력을 `BattleRoomLoader.Update`에 연결하고, 중복 스킬 입력은 코루틴 시작 시 배열 스냅샷으로 고정해 다음 프레임의 리스트 재사용으로 사라지지 않게 했다.
- Verification: `Assembly-CSharp.csproj` MSBuild exit code 0. 기존 경고만 남고 신규 컴파일 오류는 없다.
