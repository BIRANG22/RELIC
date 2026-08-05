# 배틀 맵 UI 마감 설계

## 목표

- 연결선이 40x40 노드의 중심이 아니라 경계까지만 이어지게 한다.
- 보스 노드 오른쪽에 남는 불필요한 한 화면 크기 여백을 제거한다.
- MapPanel/CharacterInfo의 Character1~3에 현재 파티 HP와 아이콘을 표시한다.
- 다음 노드 선택 버튼을 아이콘 전용 프리팹으로 분리한다.

## 설계

- `MapLineView`는 선 방향의 양 끝에서 `20`만큼 안쪽으로 이동한 두 점을 사용한다. 가로선과 대각선 모두 동일한 경계 규칙을 적용한다.
- 스크롤 Content 너비는 `max(viewportWidth, nodeSpan + horizontalPadding)`으로 계산한다. 현재 NodeRoot/LineRoot의 좌측 20 오프셋과 패딩 40을 조합해 보스 뒤에는 약 20만 남긴다.
- `BattleMapPartyInfoPresenter`는 파티 슬롯 0~2의 CharacterId로 `CharacterRuntimeStore`를 조회한다. HP는 `CurrentHP/MaxHP`, 아이콘은 `CharacterIconDatabase.TryGetIcon` 결과를 표시하며 빈 슬롯은 숨긴다. 지도 패널이 열릴 때 갱신한다.
- `NextNodeChoicePrefab`은 Button, 배경 Image, NodeIcon Image, `BattleNextNodeChoiceButton`만 가진다. 선택 패널이 부족한 슬롯을 프리팹으로 생성하며 NodeType/MapId 텍스트는 생성하지 않는다.

## 멀티플레이 경계

표시 계층은 기존 RuntimeStore의 읽기 전용 스냅샷만 사용하며 전투 상태를 변경하지 않는다. 노드 선택 결과 전달은 기존 NodeIndex 기반 경로를 유지한다.
