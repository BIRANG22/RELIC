# Lobby Quest Bootstrap Design

## Goal

로비 씬에 직접 배치된 퀘스트 패널 의존을 제거하고, 부트스트랩에서 생성되는 퀘스트 매니저와 퀘스트 패널로 튜토리얼형 로비 진행을 강제한다.

## Current Findings

- `LobbyTutorialController`가 대화, 시작 룬 지급, 퀘스트 텍스트 표시, 저장 진행도 변경을 함께 담당한다.
- `QuestPanel`은 `Lobby.unity`의 `PositionPanel` 아래에 직접 배치되어 있고, `LobbyTutorialController`가 해당 패널과 `QuestText`를 참조한다.
- 진행 상태는 `LobbyRuntimeData.TutorialProgress`의 `NotStarted`, `WaitingForSetup`, `FirstExpeditionAssigned`, `Completed`를 이미 사용한다.
- 로비 행동 진입점은 `LobbyPanelTransitionButton`, `PositionStageSelectController`, `BattlePlayButton`, `LobbyElricInteraction` 등 여러 스크립트에 분산되어 있다.

## Recommended Architecture

### Runtime Progress

기존 저장 필드 `LobbyRuntimeData.TutorialProgress`를 계속 사용한다. 새 저장 필드는 만들지 않는다. 퀘스트 매니저는 이 값을 읽어 현재 퀘스트 텍스트와 해금 상태를 계산하고, 퀘스트 완료 조건이 충족되면 다음 진행도로 넘긴다.

### Bootstrap-Owned Quest System

`Bootstrap`은 데이터와 세이브 로드가 끝난 뒤 `LobbyQuestManager`를 보장한다. 매니저는 `DontDestroyOnLoad` 객체로 유지되고, 로비 씬이 활성화되면 퀘스트 패널을 생성하거나 활성화한다.

`LobbyQuestPanel`은 매니저에서 받은 텍스트와 표시 여부만 반영한다. 퀘스트 조건 계산이나 버튼 잠금 판단은 하지 않는다.

### Interaction Gate

`LobbyQuestGate`는 버튼 또는 월드 오브젝트 클릭 스크립트가 실행 전에 호출할 수 있는 작은 게이트 컴포넌트다. 각 게이트는 최소 요구 진행도와 잠금 메시지를 갖고, 현재 진행도가 부족하면 실행을 막는다.

기본 해금 흐름:

- `NotStarted`: 엘릭 대화만 허용한다.
- `WaitingForSetup`: 캐릭터/파티/세팅 관련 진입을 허용한다. 탐험 시작과 스테이지 선택은 잠근다.
- `FirstExpeditionAssigned`: 스테이지 선택과 탐험 시작을 허용한다.
- `Completed`: 전체 로비 기능을 허용한다.

### Cleanup

로비 씬에 직접 배치된 `QuestPanel`은 더 이상 사용하지 않도록 제거 또는 비활성화한다. `LobbyTutorialController`의 퀘스트 패널 필드와 퀘스트 표시 책임은 제거하고, 대화와 시작 룬 지급, 튜토리얼 진행도 변경만 남긴다.

사용하지 않는 퀘스트 표시 메서드와 자동 탐색 코드는 제거한다. 단, `LobbyTutorialController` 자체는 엘릭 대화와 진행도 전환에 여전히 필요하므로 제거하지 않는다.

## Test Strategy

- EditMode 테스트로 진행도별 게이트 허용 여부를 검증한다.
- EditMode 테스트로 퀘스트 텍스트 모델이 진행도와 보유 아이템에 따라 올바르게 계산되는지 검증한다.
- 소스 검증으로 `LobbyTutorialController`가 더 이상 `QuestPanel`을 직접 참조하지 않는지 확인한다.
- Unity 에디터는 항상 열려 있다는 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다.

## Multiplayer Impact

이번 변경은 로비 UI 진행 제한과 저장된 튜토리얼 진행도만 다룬다. 전투 결과 계산, 랜덤, 네트워크 동기화 규칙은 변경하지 않는다. 멀티플레이에서는 호스트/클라이언트 전투 시작 권한 검사는 기존 `BattlePlayButton`과 Steam 동기화 계층을 그대로 통과한 뒤 적용된다.
