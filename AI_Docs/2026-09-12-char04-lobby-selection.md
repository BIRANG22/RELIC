# Char_04 로비 선택 연결

## 목적

파티 최대 인원은 3명을 유지하면서, 로비 캐릭터 설정 패널에서 `Char_04`(이네스)를 기존 파티 슬롯 중 하나에 편성하고 전투에 투입할 수 있게 한다.

## 설계

- `Lobby.unity`에 이미 배치된 빈 잠금 버튼 `CharBtn_3`을 이네스 선택 버튼으로 사용한다.
- 버튼의 `characterId`를 `Char_04`로, 로비 선택 분류를 `CharacterType.Cross`로 설정하고 잠금을 해제한다.
- `CharPick`은 캐릭터 ID 기반으로 기존 3개 파티 슬롯에 편성한다. 파티 인원 제한은 변경하지 않는다.
- 전투 프리팹은 `DataBootstrap`이 `CharacterPrefabDB`의 `Char_04` BattlePrefab을 캐릭터 데이터에 주입한 뒤, 기존 전투 로더가 파티 ID 기준으로 생성한다.

## 검증 범위

- 로비 씬의 이네스 버튼이 `Char_04`와 해금 상태로 직렬화됐는지 확인한다.
- `CharacterPrefabDB`의 `Char_04`가 BattlePrefab, Preview UI, Rest, Battle Event 프리팹을 가지는지 확인한다.
- C# 컴파일을 수행한다. Unity 에디터 수동 플레이 검증은 사용자 에디터에서 수행한다.
