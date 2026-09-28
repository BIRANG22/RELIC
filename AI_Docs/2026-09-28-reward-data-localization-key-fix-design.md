# 보상 데이터 로컬라이제이션 키 연결 수정 설계

## 목표

스킬(기억) 또는 유물 획득 UI가 보상 ID를 기준으로 최신 GameData를 다시 조회하고, 이름·효과·레어도를 현재 언어의 올바른 키로 표시한다.

## 원인

- 에디터 GameData 스캐너는 `SkillMaster`를 `skillmaster`로 정규화하지만 런타임은 `skill_master`로 정규화한다.
- `SkillMaster`의 사용자 표시용 `효과` 열은 현재 스캐너의 대상 열 및 `details` 필드로 인식되지 않는다.
- 획득 패널의 이름은 `RewardId`로 다시 조회하지 않고 `BattleRewardData.Name`에 캐시된 값을 사용한다.
- 유물 레어도는 ID별 데이터 문장이 아니라 공통 등급 값인데도 존재하지 않는 `data.relic.{id}.rarity` 키를 조회한다.

## 설계

1. `LocalizationProjectScanner`의 GameData 키 생성은 런타임 `GameLocalization.BuildDataKey`를 사용한다.
2. `SkillMaster`의 `효과`/`Details` 열을 `details` 필드로 명시적으로 매핑한다.
3. `BattleRewardEquipPanelUI.RefreshItemInfo`는 `RewardId`로 스킬 또는 유물을 조회한 뒤 이름·효과·레어도를 다시 계산한다.
4. 스킬 레어도는 기존 `SkillRarityUtility`의 공통 키를 유지한다.
5. 유물 레어도는 `RelicRarityUtility`로 정규화한 뒤 `common.rarity.*` 공통 키를 사용한다.
6. 데이터가 없거나 등급을 파싱할 수 없는 경우 기존 보상 문자열 또는 원본 데이터 값을 안전한 fallback으로 사용한다.

## 검증

- 스캐너가 camel-case 시트명을 런타임과 같은 snake_case 키로 생성하는지 EditMode 테스트로 검증한다.
- `SkillMaster` 효과 열이 `details` 키가 되고 사용자 표시 열로 인식되는지 검증한다.
- 공통 유물 레어도 키 선택 및 알 수 없는 값의 fallback을 검증한다.
- 런타임/에디터 C# 컴파일과 관련 EditMode 테스트를 검증한다.

## 멀티플레이 경계

표시용 데이터 조회와 텍스트 키 선택만 변경한다. 보상 판정, 인벤토리 상태, 동기화 ID 및 전투 결과에는 영향을 주지 않는다.
# 후속 원인 보완: Equip_panel/item 동적 텍스트 소유권

`Equip_panel/item/Name`, `Rarity`, `Effect`, `Cost_Value`는 선택된 보상 ID로 조회한 데이터를 패널이 매번 출력하는 동적 텍스트다. 씬에 남아 있던 `LocalizedTMPText`가 이 값을 정적 UI 키로 다시 덮어쓰고 있었으며, 특히 `Cost_Value`에는 `common.rarity`가 잘못 연결되어 숫자 대신 "레어도"가 표시되었다.

네 오브젝트의 정적 로컬라이저를 `LocalizationIgnore`로 교체하고, 런타임 자동 보호에도 `Cost_Value`를 포함한다. 이름·레어도·효과는 기존 ID 기반 데이터 조회 결과를 사용하고 비용은 `SkillMasterData.ResourceCostValue` 숫자를 그대로 표시한다.

## 원본 프리팹 및 SkillMaster 번역 테이블 보완

씬 인스턴스뿐 아니라 `Equip_panel.prefab`의 `Name`, `Rarity`, `Effect`도 동적 출력으로 고정한다. `Battletest`처럼 원본 프리팹을 직접 사용하는 씬에서도 정적 `ui.equip_panel.*` 키가 ID 기반 결과를 덮어쓰지 않아야 한다.

SkillMaster 번역 키는 런타임과 동일한 `data.skill_master.{skillId}.name/details`만 사용한다. 기존 `data.skillmaster.*.name` 행의 번역값은 키만 마이그레이션하여 보존하고, 모든 SkillMaster 데이터 행에 이름과 효과 키가 존재하도록 누락 행을 추가한다. 새 효과 행은 GameData의 한국어 `Details`를 원문으로 사용하며 다른 언어 번역값은 임의 생성하지 않는다.

## 전역 동적 자동 바인딩과 presenter 출력 분리

`LocalizationIgnore`는 정적 바인딩만 제외하고 `RuntimeTMPTextAutoLocalizer`의 동적 원문 매칭은 허용한다. 이 때문에 Equip_panel의 직렬화 플레이스홀더가 먼저 동적 키로 기억된 뒤, 패널이 쓴 ID 기반 번역을 비동기 갱신에서 다시 덮어썼다.

이미 최종 번역값을 계산하는 presenter 출력에는 `LocalizationAutoBindingIgnore`를 추가한다. 전역 자동 로컬라이저는 이 표식 아래에 작성자를 추가하지 않으며, 이전에 붙은 `DynamicLocalizedTMPText`도 값을 재적용하지 않는다.

## 보상 표시 데이터 복원 보완

- 비어 있는 번역 셀과 미완성 GameData 원문은 이번 범위에서 수정하지 않는다.
- 장비 패널은 일시적인 `BattleRewardData.Icon` 캐시에 의존하지 않고 보상 ID로 스킬·유물 아이콘 DB를 다시 조회한다. 저장 데이터에서 복원된 보상도 동일하게 처리한다.
- 스킬 레어도 번역에는 의미에 맞는 한국어 fallback을 명시하여 한국어 환경에서 영어 `Untranslated`가 표시되지 않게 한다.
- 코스트 숫자는 현지화를 거치지 않고 동일한 `SkillMasterData.ResourceCostValue`에서 계속 가져온다.
