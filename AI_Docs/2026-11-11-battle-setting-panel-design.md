# 전투 설정 패널 런타임 정보 연동 설계

## 목표

- `Battle` 씬의 `SettingButton`을 누르면 기존 `Setting_panel`을 연다.
- 패널에 현재 파티 3명의 전투 런타임 상태를 읽기 전용으로 표시한다.
- 캐릭터 이름/초상, 현재 HP, 패시브, 장착 기억 3개, 액티브 유물, 아티팩트 6개, 룬 6개를 연동한다.
- `CheckButton`을 누르면 패널을 닫는다.

## 데이터 흐름

`PartyRuntimeStore`의 파티 슬롯 ID를 기준으로 `CharacterRuntimeStore`에서 `CharacterRuntimeData`를 조회한다. UI는 이 스냅샷을 표시만 하며 장비나 전투 상태를 변경하지 않는다.

- HP: `CurrentHP / BattleCharacterPanelUI.ResolvePartyMaxHP(runtime)`
- 마나: `CurrentCost / BattleCharacterPanelUI.ResolvePartyMaxCost(runtime)`
- 마나 회복량: `TotalCostRecovery`
- 카르마: `CurrentResource`를 0~5 범위의 아이콘 점등으로 표시
- 패시브: `PassiveSkillId`
- 기억: `EquippedSkillIds[1..3]` (첫 슬롯이 비었으면 `AbilitySkillId`를 보조값으로 사용)
- 궁극기: `UniqueSkillId`
- 액티브 유물: `EquippedRelicIds[0]`
- 아티팩트: `EquippedRelicIds[1..6]`
- 룬: `EquippedRuneIds[0..5]`

## UI 구조

항상 활성인 `SettingButton`에 `BattleSettingPanelUI`를 연결한다. 비활성 상태로 시작하는 `Setting_panel` 자체에 컨트롤러를 두지 않아도 열기 버튼 리스너가 정상 등록된다. 기존 `Char_content/Char1..3`의 하위 슬롯과 이미지를 찾아 갱신하며 새 UI 오브젝트는 생성하지 않는다.

패널이 열려 있는 동안 런타임 스냅샷 해시가 달라질 때만 다시 그린다. 이는 표시 전용 처리이며 전투의 `Command -> State Change -> Result/Event` 흐름에 관여하지 않는다.

## 설정 버튼 토글 및 연출

`SettingButton`은 `BagButton`이 사용하는 공용 버튼 연출 컴포넌트를 동일한 값으로 재사용한다. 버튼의 기존 자식 콘텐츠를 대상으로 호버 시 1.1배 확대(0.2초), 클릭 상태 토글 유지, 다른 버튼 호버 시 클릭 상태 해제를 적용한다. 새 애니메이션 코드는 만들지 않는다.

버튼 클릭은 `Setting_panel`의 현재 활성 상태를 반전한다. 닫힌 상태에서는 패널을 열고 최신 파티 데이터를 갱신하며, 열린 상태에서는 다시 클릭하면 닫는다. `CheckButton`의 닫기 동작도 유지한다.

각 캐릭터 카드의 `Skill/Content/Ultimate/Icon`은 장착 기억과 분리하여 `CharacterRuntimeData.UniqueSkillId`의 아이콘을 표시한다.

설정 패널은 확정된 현재 상태를 표시하므로 예약 행동을 반영하는 `PreviewCost`와 `PreviewResource` 대신 `CurrentCost`와 `CurrentResource`를 사용한다.

마나 텍스트는 공백 없는 `현재/최대` 형식으로 표시한다. `Mana/Value`의 폭은 80으로 두고 TMP 줄바꿈을 비활성화하여 최대 `99/99`까지 한 줄로 유지한다.

## 장착 정보 및 자원 이름 툴팁

`BattleSettingPanelUI`는 패널이 열려 있을 때만 각 슬롯의 `RectTransform`과 현재 스냅샷의 안정적인 데이터 ID를 연결한다. 포인터가 들어온 슬롯이 바뀌면 ID로 Database를 조회해 기존 `Setting_panel/TooltipPanel` 또는 `Setting_panel/TooltipUI`를 갱신하며 전투 상태는 변경하지 않는다.

- 패시브, 궁극기, 기억: `SkillDatabase`에서 조회해 `TooltipPanel`에 이름, 분류, 설명, 엔트리, 범위, 자원 정보를 표시한다.
- 룬: `RuneDatabase`의 지역화된 이름과 `ValueRate`/`CountRate`가 적용된 설명을 `TooltipUI`에 표시한다.
- 유물: `RelicDatabase`의 지역화된 이름과 효과 설명을 `TooltipUI`에 표시한다.
- 연성제: `CompoundDatabase`의 지역화된 이름과 `ValueRate`/`CountRate`가 적용된 설명을 `TooltipUI`에 표시한다.
- 마나, 마나재생량, 카르마: 각 자원 아이콘 영역을 호버하면 로비 `Stats_Area/Tooltip`과 같은 150x15 이름 전용 텍스트를 표시한다. 기존 정보용 `TooltipUI`는 사용하지 않으며 각각 `common.mana`, `common.recovery`, `resource.karma`의 지역화 문자열을 사용한다. 카르마의 다섯 아이콘은 모두 같은 설명을 사용한다.

빈 ID의 장착 슬롯에는 툴팁을 표시하지 않는다. 포인터가 슬롯을 벗어나거나 패널이 닫히면 툴팁과 연결된 효과 설명 툴팁도 숨긴다. 툴팁은 호버 대상 오른쪽에 배치하되 캔버스 경계를 넘으면 안쪽으로 보정한다.

스킬 툴팁은 실제 전투 스킬 슬롯과 동일하게 `SkillType`/빠른 스킬 여부를 지역화한 한국어 타입, `SkillRangeIconDatabase`의 범위 이미지, 전투 캐릭터 패널의 자원 이미지와 색상, 스킬 희귀도 선 색상을 표시한다. 상세 설명은 기존 `SkillDescriptionFormatter`와 인라인 효과 링크 형식을 유지한다. `TooltipPanel/Detail`에 있는 밑줄 효과 링크는 기존 `UnitStatusEffectTooltipUI`로 연결하여 상세 효과 정보를 계속 확인할 수 있게 한다.

### 검토한 접근

- 선택: 컨트롤러가 열려 있는 동안 슬롯 영역을 직접 판정하고 기존 두 툴팁을 재사용한다. 런타임 `AddComponent`와 대량의 씬 이벤트 연결을 피하면서 현재 패널 구조를 그대로 사용한다.
- 제외: 모든 아이콘에 전용 호버 컴포넌트를 자동 추가한다. 장기 구조에서 명시적 참조를 우선하는 프로젝트 규칙과 맞지 않는다.
- 보정: 마나·재생·카르마는 상세 정보 툴팁과 시각적으로 분리해야 한다는 피드백에 따라 로비 `Stats_Area` 형식의 이름 전용 텍스트를 사용한다.

## 자원 호버 판정 및 모달 표시 보정

자원 이름 호버는 장착 슬롯과 같은 공통 역순 목록에 의존하지 않는다. 각 캐릭터의 `Resources/Mana`, `Resources/Regen`, `Resources/Karma` 영역을 종류별로 명시적으로 연결하고 자원 영역을 장착 정보보다 먼저 판정한다. 마나는 `common.mana`, 재생량은 `common.recovery`, 카르마는 `resource.karma`로 고정한다.

`Setting_panel`은 읽기 전용이지만 전투와 보상 UI 위에 뜨는 모달이다. 열릴 때 자신의 Canvas를 override sorting으로 설정하고 `MenuPanel`의 튜토리얼 최대 정렬값 30003보다 높은 30004를 사용하며 마지막 형제로 이동한다. 전체 화면 Image와 GraphicRaycaster로 뒤쪽 입력을 차단한다.

패널이 열릴 때 현재 `Time.timeScale`을 저장하고 열린 동안 0으로 유지한다. 닫을 때 저장한 값을 복원하되 `MenuPanel`이 여전히 열려 있으면 0을 유지하여 다른 모달의 일시정지를 해제하지 않는다. 이 처리는 연출 정지 전용이며 전투 상태 계산에는 관여하지 않는다.

### 중첩 Canvas 호버 좌표 보정

`Setting_panel` 자체 Canvas는 씬 YAML에서 World Space이면서 카메라 참조가 비어 있다. 자원 슬롯이 실제로 속한 화면 좌표계는 상위 Battle UI Canvas이므로, 가장 가까운 Canvas의 `renderMode`를 사용하면 잘못된 카메라로 `RectangleContainsScreenPoint`를 호출하게 된다. 모든 호버 판정과 툴팁 화면 좌표 변환은 `Canvas.rootCanvas`를 기준으로 이벤트 카메라를 결정한다. 최상위 Canvas가 Screen Space Overlay이면 null, 그 외에는 최상위 Canvas의 `worldCamera`를 사용한다.

### 자원 이름 툴팁 이벤트 소유권 보정

자원 이름 툴팁은 로비 `CharacterStatsAreaNameTooltip`과 같은 `PointerEnter`/`PointerExit` 방식으로 현재 아이콘을 명시적으로 추적한다. 마나·재생량은 각 아이콘을 대상으로 하고, 카르마는 다섯 아이콘을 모두 같은 `resource.karma` 키로 연결한다. 새 아이콘에 진입한 뒤 이전 아이콘의 늦은 종료 이벤트가 도착해도 현재 아이콘과 일치할 때만 툴팁을 닫아 최신 호버를 보존한다.

활성 아이콘의 지역화 키는 표시 중 계속 다시 적용하여 언어 변경과 이전 텍스트 잔류를 방지한다. 자원 이름 선택에는 더 이상 수동 `RectangleContainsScreenPoint` 폴링을 사용하지 않으며, 장착 정보 툴팁과 효과 링크의 기존 좌표 계산은 유지한다.

#### 검토한 접근

- 선택: 로비와 동일한 포인터 이벤트 기반 소유권 관리. 실제 UI 이벤트 대상과 표시 상태가 일치하고 이벤트 순서가 교차해도 최신 대상을 유지한다.
- 제외: 기존 사각형 폴링에서 매 프레임 텍스트만 재설정. 잘못된 자원 대상이 선택된 상태를 해결하지 못한다.

### 검토한 접근

- 선택: 기존 공용 버튼 연출 컴포넌트 재사용. `BagButton`과 동일한 시각 동작을 유지하고 중복 코드가 없다.
- 제외: `BattleSettingPanelUI`에 전용 애니메이션 구현. 기존 연출과 설정이 갈라지고 유지보수 비용이 증가한다.

