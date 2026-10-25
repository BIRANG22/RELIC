# ExplorationResultPanel 장식 이미지 자리 오브젝트 설계

## 목표

`Battle.unity`의 `ExplorationResultPanel`에 아트가 아이콘 및 사각 라인 이미지를 연결할 수 있는 고정 UI 오브젝트를 추가한다.

## 조사 결과

- `ExplorationReportStageGroup`에는 소요 시간과 세 종류의 노드 입장 횟수 라벨/값이 세로로 배치되어 있다.
- `ExplorationReportRow_0~2`는 각 캐릭터의 초상, 이름, 레벨, 경험치 정보를 담는 카드 루트다.
- 획득 아이템 라벨, 아이템 슬롯, `RedDustium`은 공통 부모인 `ExplorationReportFrame` 아래에 있다.
- 요청한 장식은 고정 UI 구성 요소이므로 런타임 생성보다 씬에 직접 배치하는 편이 프로젝트의 Inspector/Data Workflow 규칙에 맞는다.
- 사용할 실제 Sprite가 지정되지 않았으므로 기본 `Image`가 흰색 사각형으로 노출되지 않게 최초 상태는 비활성화해야 한다.

## 권장 설계

### 스테이지 요약 아이콘

`ExplorationReportStageGroup` 아래에 40x40 `Image` 자리 오브젝트 네 개를 추가한다.

- `ExplorationReportElapsedTimeIcon`: (-735, 290)
- `ExplorationReportNormalBattleIcon`: (-735, 210)
- `ExplorationReportEliteBattleIcon`: (-735, 130)
- `ExplorationReportEventIcon`: (-735, 50)

각 오브젝트는 대응 라벨의 왼쪽에 놓고, `Image.enabled=false`, `raycastTarget=false`로 시작한다. 아트가 Sprite를 연결한 뒤 `Image`를 활성화해 사용한다.

### 캐릭터 정보 라인

각 `ExplorationReportRow_0~2`의 첫 번째 자식으로 아래 오브젝트를 추가한다.

- `ExplorationReportRowFrame_0`
- `ExplorationReportRowFrame_1`
- `ExplorationReportRowFrame_2`

부모 영역에 Stretch하고 `sizeDelta=(20, 20)`을 적용해 카드보다 사방 10px 크게 만든다. 첫 번째 자식으로 두어 활성화 시 캐릭터 정보 뒤에 렌더링한다.

### 획득 보상 영역 라인

`ExplorationReportFrame`의 첫 번째 자식으로 `ExplorationReportRewardFrame`을 추가한다. 위치는 (130, -275), 크기는 880x190으로 설정해 획득 아이템 라벨, 아이템 슬롯, `RedDustium`을 함께 감싼다.

## 범위

- 씬 UI 계층과 이미지 자리 오브젝트만 추가한다.
- 실제 Sprite 제작 및 연결은 포함하지 않는다.
- 전투 결과 계산, 보상 데이터, 네트워크 상태에는 영향을 주지 않는다.
