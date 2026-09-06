# 로비 스킬 강화 패널 설계

## 목표

배틀 씬의 기존 `UpgradePanel`과 `SkillUpgradePanel`을 수정하지 않고, 동일한 외형과 스킬 강화 경험을 로비 `PositionPanel`에서 제공한다.

## 범위

- 배틀 씬과 배틀 씬 내부 업그레이드 패널은 읽기만 하고 수정하지 않는다.
- 배틀 업그레이드 패널의 UI 계층을 로비 전용 프리팹으로 복사한다.
- 로비 `PositionPanel`에 `RelicOffer_3` 오른쪽, 기준 X 약 730 위치에 강화 버튼을 둔다.
- 강화 버튼은 로비 강화 패널을 열고, 패널의 별도 닫기 버튼만 패널을 닫는다.
- 강화 후 자동으로 닫지 않는다.

## 데이터와 가격

- 강화 대상은 현재 파티 캐릭터의 강화 가능한 장착 스킬과 `LobbyRuntimeData.SkillInventoryIds`이다.
- 결제 재화는 `LobbyRuntimeData.BlueDustium`이다.
- 첫 강화 가격은 100이며 성공할 때마다 50씩 증가한다.
- 가격 공식은 `100 + LobbySkillUpgradeCount * 50`이다.
- 성공한 강화만 BlueDustium을 차감하고 `LobbySkillUpgradeCount`를 1 증가시킨다.
- 강화 횟수 제한은 없다.
- 저장 버튼을 누를 때만 SaveSystem에 영구 저장되며 자동 저장하지 않는다.
- 새 게임에서 `LobbySkillUpgradeCount`는 0으로 초기화된다.

## 구조

- `LobbySkillUpgradeService`: 가격 검증, 재화 차감, ID 기반 스킬 교체, 결과 반환을 담당한다.
- `LobbySkillUpgradePanelUI`: 현재 파티와 로비 스킬 인벤토리를 읽고 아이콘, 상세 정보, 현재 가격을 표시한다.
- `LobbySkillUpgradeEntryUI`: 기존 `SkillUpgradeIconItem` 프리팹을 활용해 선택 요청을 전달한다.
- 에디터 변환 도구: 배틀 씬의 UI 계층을 복사해 로비 전용 프리팹을 만들고 로비 씬에 버튼과 패널을 배치한다. 배틀 씬은 저장하지 않는다.

## 실패 처리

- DataManager, 스킬 데이터, 캐릭터 ID, 슬롯 인덱스가 유효하지 않으면 상태를 변경하지 않는다.
- BlueDustium이 부족하면 상태를 변경하지 않고 실패 결과를 반환한다.
- 이미 강화형인 스킬이나 강화 대상 ID가 없는 스킬은 목록에 표시하지 않는다.

## 검증

- 가격이 100, 150, 200 순서로 증가하는지 검사한다.
- 잔액 부족 시 재화, 스킬, 누적 횟수가 변하지 않는지 검사한다.
- 캐릭터 장착 스킬과 로비 인벤토리 스킬 강화가 각각 올바른 ID를 변경하는지 검사한다.
- 배틀 씬 파일이 작업 전후 변경되지 않았는지 확인한다.
- 로비 씬에 강화 버튼과 로비 전용 업그레이드 프리팹 인스턴스가 있는지 확인한다.

