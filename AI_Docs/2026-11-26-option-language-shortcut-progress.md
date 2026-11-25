# 실행 기록 — AI_Docs/2026-11-26-option-language-shortcut-plan.md

- Ruling: 현재 브랜치/작업 공간에서 작업하라는 프로젝트 규칙과 worktree 생성 사전 승인 규칙이 실행 스킬의 격리 workspace 지침보다 우선하므로 현재 작업 공간을 사용한다. 잘못된 판단이면 다른 변경과의 충돌 위험이 있으나 현재 작업 트리는 시작 시 깨끗했다.
- Ruling: 커밋은 사용자 승인이 없으므로 수행하지 않는다. 실행 기록은 이 문서와 실제 diff로 유지한다.
- Pre-flight: Task 1의 `BattleShortcutAction`/조회/변경 이벤트를 Task 3과 Task 4가 공통 소비하며 명칭을 계획과 동일하게 유지한다.
- Task 1 Ruling: `Assets/Tests/EditMode~/`는 생성된 csproj에서 의도적으로 제외되어 MSBuild만으로 RED 실행이 불가능하다. Unity 직접 제어/batchmode 금지 규칙을 지키기 위해 테스트는 Unity EditMode용으로 작성하되 RED/GREEN 실행은 미검증으로 기록하고, 런타임 어셈블리 컴파일과 정적 YAML 검증으로 보완한다.
- Task 1 Ruling: Unity가 생성한 csproj가 새 런타임 파일을 즉시 포함하지 않아 독립 파일은 MSBuild 검증에서 누락되었다. 생성 파일을 직접 수정하지 않고 기존 컴파일 단위인 `BattleCharacterPanelUI.cs`에 바인딩 저장소를 배치했다.
- Task 2: 언어 값은 패널 활성화 직후에도 현재 `SelectedLocale`에서 다시 계산하며 `ProtectDynamicOwnership`으로 오래된 정적 번역 작성자를 비활성화한다. 탭/옵션 동적 텍스트도 현재 언어로 즉시 갱신한다.
- Task 3: 비어 있던 `ControlContent`는 옵션 프리팹 인스턴스 내부에 런타임 구성한다. 9개 항목을 키보드 입력으로 변경하며 중복을 허용하고 중복된 모든 행을 빨간색으로 표시한다.
- Task 4: 배틀 입력의 조기 반환을 제거해 같은 키가 배정된 동작들을 모두 평가한다. Battle/DebugBattle의 `KeyText` 중 기본 단축키 9종만 바인딩해 설정 변경 이벤트에 맞춰 갱신하고 숫자 타임라인 표기는 제외한다.
- Verification: `Assembly-CSharp.csproj` MSBuild exit code 0. 기존 `System.Net.Http` 버전 충돌 및 미할당 serialized field 경고는 남아 있으나 신규 컴파일 오류는 없다.

