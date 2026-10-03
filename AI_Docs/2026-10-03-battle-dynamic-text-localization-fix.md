# 배틀 동적 텍스트 번역 누락 수정

## 조사 결과

- `HUDSlot(Clone)/NameText`는 몬스터 이름과 달리 캐릭터 이름에 원본 `CharacterMasterData.Name`을 직접 사용했다.
- `BattleCharacterPanel/TooltipPanel/Type/Type_text`는 스킬 분류를 한국어 문자열로 직접 반환했다.
- `ShopPanel/Shop/Content/Goods01~04/Plate/Plate_Text`는 상품 종류를 한국어 문자열로 직접 설정하면서, 정적 `LocalizedTMPText(lobby.memory)`도 같은 텍스트를 갱신해 두 작성자가 충돌했다.
- 세 UI 모두 번역 테이블의 비동기 준비가 끝난 뒤 현재 표시 내용을 다시 계산하는 경로가 없거나 부족했다.

## 적용 설계

- 캐릭터·몬스터·스킬·유물처럼 ID가 있는 데이터는 `GameDataLocalization`을 통해 표시한다.
- 스킬 타입과 상품 종류 같은 공통 UI 용어는 `GameLocalization` 키로 표시한다.
- 런타임 데이터가 소유하는 TMP 텍스트에는 자동·정적 로컬라이저가 덮어쓰지 못하도록 동적 텍스트 경계를 적용한다.
- `LocalizationRuntimeRefreshCoordinator.LocaleTableReady` 수신 시 현재 바인딩된 데이터만 다시 표시한다.
- 스킬 타입용 `common.attack`, `common.buff`, `common.debuff`를 한국어·영어·중국어 간체·일본어·스페인어 테이블에 추가한다.

## 검증 범위

- 세 표시 코드의 번역 API 사용 여부 및 로케일 준비 이벤트 구독 여부를 EditMode 회귀 테스트로 고정한다.
- 번역 워크북에서 스킬 타입 키 다섯 개의 모든 지원 언어 값이 채워졌는지 검사한다.
- 런타임 어셈블리와 에디터 어셈블리를 컴파일한다.
