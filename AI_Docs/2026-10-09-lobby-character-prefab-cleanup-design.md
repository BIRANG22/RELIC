# 로비 캐릭터 프리팹 및 레거시 정리 설계

## 목표

`Lobby.unity`에 직접 배치된 `Cha_01_idle_0`~`Cha_04_idle_0` 인스턴스를 제거하고, `CharacterPrefabDB.LobbyPrefab`을 통해 같은 월드 캐릭터를 생성한다. 현재 위치, 스케일, 좌우 반전, 렌더 정렬, 이름 호버, 조명 호버, 클릭 후 캐릭터 설정창 열기 동작은 유지한다.

동시에 A~C 캐릭터 폴더의 미사용 프리팹 12개와 직렬화 참조가 없는 레거시 `PartySlot` 계열을 제거한다. 로비 씬의 비활성 오브젝트는 비활성 상태만으로 삭제하지 않고, 씬 참조와 코드 활성화 경로가 모두 없는 경우에만 제거한다.

## 조사 결과

- GUID 외부 참조가 없는 프리팹: `c1`, `c2`, `c3`, `Cha_A`, `Cha_B`, `Cha_C`.
- `A_Robby_idle`, `B_Robby_idle`, `C_Robby_idle`은 `PreviewWorldPrefab`에만 연결되어 있으며 `TryGetPreviewWorldPrefab` 호출처가 없다.
- `LobbySlot_ACharacter`, `LobbySlot_BCharacter`, `LobbySlot_CCharacter`는 기존 `LobbyPrefab`에만 연결되어 있다.
- `PartySlot`과 `PartySlotButton` 스크립트는 어떤 씬이나 프리팹에도 직렬화되어 있지 않다. 남은 호출은 빈 검색 결과를 순회하거나 과거 호환 폴백으로 동작한다.
- 로비 월드의 네 캐릭터는 `BackGround/Character` 아래에 직접 배치되어 있고, 씬 오버라이드로 `LobbyWorldObjectHoverName`과 `UIHoverLight2DFalloff`를 사용한다.
- `Lobby.unity`의 초기 비활성 오브젝트에는 설정창, 튜토리얼, 선택 강조 등 정상적으로 런타임에 활성화되는 항목이 다수 포함되어 있다.

## 권장 구조

### DB 역할

- `BattlePrefab`: 전투 유닛 프리팹.
- `LobbyPrefab`: 로비 월드에 표시할 캐릭터 프리팹.
- `PreviewUIPrefab`: 캐릭터 선택 UI 미리보기.
- `RestPrefab`: 휴식방 캐릭터 프리팹.
- `BattleEventWorldPrefab`: 배틀 시작/맵 이벤트 월드 프리팹.

미사용 `PreviewWorldPrefab` 필드와 조회 메서드는 제거한다. `TryGetBattleEventWorldPrefab`은 명시적인 `BattleEventWorldPrefab`만 반환한다.

### 로비 월드 생성

`LobbyWorldCharacterSettingOpenObject`가 다음 역할을 담당한다.

1. 직렬화된 `CharacterId`별 배치 설정을 순회한다.
2. `CharacterPrefabDB.TryGetLobbyPrefab`으로 월드 프리팹을 조회한다.
3. 설정된 위치, 회전, 스케일을 적용한다.
4. SpriteRenderer별 좌우 반전과 sorting layer/order를 현재 씬 배치값과 동일하게 적용한다.
5. 이름 호버와 조명 호버를 구성하고 기존 클릭 릴레이를 바인딩한다.
6. 중복 초기화 시 기존 런타임 인스턴스를 정리한 뒤 한 번만 다시 생성한다.

배치값과 연출 옵션은 인스펙터에 노출한다. 캐릭터 식별은 오브젝트 이름이 아니라 `CharacterId`를 우선한다.

### 레거시 정리

삭제할 프리팹 12개:

- `A/c1.prefab`, `A/Cha_A.prefab`, `A/A_Robby_idle.prefab`, `A/LobbySlot_ACharacter.prefab`
- `B/c2.prefab`, `B/Cha_B.prefab`, `B/B_Robby_idle.prefab`, `B/LobbySlot_BCharacter.prefab`
- `C/c3.prefab`, `C/Cha_C.prefab`, `C/C_Robby_idle.prefab`, `C/LobbySlot_CCharacter.prefab`

삭제할 레거시 코드:

- `PartySlot.cs`, `PartySlotButton.cs`
- `CharBtn`, `CharPick`, `SteamLobbyPartySynchronizer`의 `PartySlot` 새로고침 루프
- `LobbyMainPanelKeyboardInputController`의 `PartySlotButton` 폴백

추가 오브젝트·스크립트는 다음 세 조건이 모두 충족될 때만 삭제한다.

1. 씬/프리팹/ScriptableObject에 직렬화 참조가 없다.
2. 코드에서 타입, 이름 또는 리소스 경로로 사용되지 않는다.
3. 런타임 초기화, 콜백, 리플렉션 진입점이 아니다.

## 오류 처리

- DB 또는 프리팹이 없으면 해당 캐릭터만 건너뛰고 `CharacterId`가 포함된 경고를 남긴다.
- 배치 설정에 빈 ID나 중복 ID가 있으면 무시하고 경고한다.
- 이름 호버 UI가 없더라도 캐릭터 생성과 클릭 동작은 유지한다.

## 검증

- EditMode 테스트로 DB 로비 조회, 배치 설정 검증, 누락/중복 처리, 재생성 시 중복 방지를 확인한다.
- 정적 GUID 검색으로 삭제 에셋 참조가 남지 않았는지 확인한다.
- `Assembly-CSharp`와 `Assembly-CSharp-Editor`를 빌드해 컴파일을 확인한다.
- Unity는 열려 있다고 가정하며 batchmode 테스트는 실행하지 않는다.
- 실제 로비에서 네 캐릭터의 위치, 렌더 순서, 호버, 클릭을 수동 확인 항목으로 남긴다.

## 멀티플레이 영향

전투 상태와 네트워크 동기화 데이터는 변경하지 않는다. 로비 월드 표현을 `CharacterId` 기반 DB 조회로 전환하며, 파티 상태 변경이나 전투 결과 계산에는 영향을 주지 않는다.
