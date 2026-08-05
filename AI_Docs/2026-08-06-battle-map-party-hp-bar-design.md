# 배틀 맵 파티 HP 바 설계

## 목표

맵 패널의 `Character1`~`Character3` 슬롯에서 HP 텍스트 바로 아래에 현재 HP 비율을 표시하는 얇은 가로 바를 제공한다.

## 설계

- 각 캐릭터 슬롯에 편집 가능한 명시적 씬 오브젝트 `HpBar/Fill`을 둔다.
- `HpBar`는 어두운 배경, `Fill`은 붉은색 Filled Image로 구성하며 왼쪽에서 오른쪽으로 채운다.
- 기존 `BattleMapPartyInfoPresenter.Render`가 텍스트·아이콘과 같은 시점에 `CurrentHP / MaxHP` 비율을 갱신한다.
- 현재 HP는 0~최대 HP 범위로 제한하며 최대 HP가 0 이하이면 비율은 0으로 표시한다.
- 두 Image의 Raycast Target은 꺼서 노드 호버와 맵 드래그를 방해하지 않는다.

## 멀티플레이 경계

전투 상태를 변경하지 않고 이미 동기화된 `CharacterRuntimeData`를 읽어 표시만 하는 UI 변경이다.
