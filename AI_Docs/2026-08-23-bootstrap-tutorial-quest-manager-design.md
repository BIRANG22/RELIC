# Bootstrap Tutorial Quest Manager Design

## 목표

튜토리얼 구간에서 처음 열리는 시스템 버튼에만 진행 강제성을 부여할 수 있는 `QuestManager` 기반을 만든다. 이번 작업은 Bootstrap 씬과 공용 스크립트/프리팹 기반만 추가하며 `Lobby.unity`, `Battle.unity`, 기존 로비 `QuestPanel`은 수정하지 않는다.

## 범위

- Bootstrap에서 유지되는 퀘스트 매니저 Host를 만든다.
- 퀘스트 진행/완료/해금 상태를 저장 가능한 `LobbyRuntimeData`에 추가한다.
- 공용 QuestPanel 프리젠터와 프리팹 기반을 만든다.
- 실제 로비 버튼 차단 연결은 다음 작업에서 수행한다.

## 구조

`Relic.Gameplay.Data.QuestManager`는 순수 런타임 서비스로 둔다. `LobbyRuntimeData`를 입력받아 현재 활성 퀘스트, 완료 퀘스트, 해금 시스템을 관리하고 `QuestActionId` 단위로 행동 가능 여부를 판정한다.

Bootstrap에는 `QuestManagerHost` MonoBehaviour를 추가한다. Host는 `Singleton<T>` 패턴으로 `DontDestroyOnLoad` 대상이 되며, `DataManager.Instance.LobbyRuntimeStore.GetOrCreate()`를 통해 `QuestManager`를 초기화한다. 저장은 직접 파일을 다루지 않고 `SaveSystem.SaveCurrentProgress()`에 위임한다.

공용 `QuestPanelPresenter`는 현재 퀘스트 표시 상태만 반영한다. 퀘스트 완료나 행동 차단 판단을 하지 않는다.

## 데이터

`LobbyRuntimeData`에 다음 필드를 추가한다.

- `ActiveQuestId`
- `CompletedQuestIds`
- `UnlockedSystemIds`

`LobbyRuntimeStore.Normalize`와 `SaveSystem.NormalizeLobby`에서 null 목록을 보정한다.

## 멀티플레이 경계

이번 작업은 로비 튜토리얼 표시/행동 게이트 기반만 만든다. 전투 결과, 랜덤, 네트워크 동기화, Battle Command/State/Event 흐름은 변경하지 않는다. 향후 배틀 결과로 퀘스트를 완료할 경우 UI가 아니라 결과 이벤트에서 `QuestManager`로 전달해야 한다.
