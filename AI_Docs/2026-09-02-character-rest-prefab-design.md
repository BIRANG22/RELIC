# 캐릭터 휴식 프리팹 연결 설계

## 목적

휴식방에서 캐릭터별 전용 휴식 프리팹을 생성할 수 있도록 `CharacterPrefabDatabase`에 `RestPrefab` 매핑을 추가한다.

## 설계

- `CharacterPrefabEntry`에 `RestPrefab` 직렬화 필드를 추가한다.
- `CharacterPrefabDatabase.TryGetRestPrefab(string characterId, out GameObject prefab)`으로 전용 휴식 프리팹을 조회한다.
- 조회 대상이 없거나 `RestPrefab`이 비어 있으면 `false`를 반환한다. `BattleEventWorldPrefab` 등 다른 프리팹으로 대체하지 않는다.
- `RestRoomController`는 캐릭터 생성 시 `TryGetRestPrefab`만 사용한다.
- `CharacterPrefabDB.asset`에는 다음 참조를 연결한다.
  - `Char_01`: `Assets/Project/PrefabsR/Character/A/hilt_rest.prefab`
  - `Char_02`: `Assets/Project/PrefabsR/Character/B/kaya_rest.prefab`
  - `Char_03`: `Assets/Project/PrefabsR/Character/C/haze_rest.prefab`
  - `Char_04`, `Char_05`: 비어 있음

## 오류 처리

휴식 프리팹이 없는 캐릭터는 현재 경고 로그를 남기고 해당 캐릭터 생성을 건너뛴다.

## 검증

- EditMode 테스트로 전용 휴식 프리팹 반환 및 미지정 시 `false` 반환을 검증한다.
- C# 프로젝트 컴파일로 API 및 사용처의 컴파일 상태를 검증한다.
- 에셋 YAML의 GUID가 각 `_rest` 프리팹 메타 GUID와 일치하는지 확인한다.

## 멀티플레이 경계

이번 변경은 캐릭터 ID를 이용한 연출 프리팹 선택만 변경한다. 전투 상태, 결과 계산, 랜덤 및 네트워크 동기화 경계에는 영향을 주지 않는다.
