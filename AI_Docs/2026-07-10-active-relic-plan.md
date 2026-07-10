# 액티브 유물 구현 계획

목표: 캐릭터당 액티브 유물 1개를 전투 스킬 리스트의 ACTION 옆에 표시하고, 내구도를 사용 가능 횟수로 사용해 효과를 실행한다.

구조:
- 첫 번째 장착 유물 슬롯(`EquippedRelicIds[0]`)을 액티브 유물 슬롯으로 사용한다.
- `GameData`에서 `Relic.Type`, `Relic.Durability`, `Character.Relic`를 로드한다.
- 남은 액티브 유물 사용 횟수는 캐릭터 런타임 데이터에 유물 ID 기준으로 저장한다.
- UI 클릭 함수가 직접 전투 상태를 바꾸지 않고 액티브 유물 서비스로 요청한다.
- 즉시형 유물은 이번 턴 상태를 적용하고, 선택형 유물은 타겟 선택 완료 후 성공했을 때만 사용 횟수를 1 소모한다.

작업:
- `Assets/Tests/EditMode~/` 아래에 데이터 매핑, 시작 유물 초기화, 사용 횟수 상태, 효과 ID 해석 테스트를 추가한다.
- `RelicData`에 `Type`, `Durability`, `CharacterMasterData`에 `Relic`, `CharacterRuntimeData`에 액티브 유물 사용 횟수 저장소를 추가한다.
- 캐릭터 런타임 생성 경로(`CharPick`, `CharBtn`)에서 캐릭터 시트의 시작 유물을 장착한다.
- 액티브 유물 서비스에서 사용 가능 여부, 즉시 사용, 그리드 타겟 확정, 아군 타겟 확정을 처리한다.
- 현재 엑셀의 `E_Value` 플레이스홀더는 코드에서 명시 효과 ID로 해석한다.
  - `Relic_11`: 이번 턴 주는 피해 증가.
  - `Relic_12`: 이번 턴 받는 피해 감소.
  - `Relic_13`: 선택 캐릭터를 선택 칸으로 이동.
  - `Relic_14`: 선택 캐릭터와 선택 아군 위치 교환.
  - `Relic_15`: 선택 칸에 `GR_Poisson` 배치.
- `Assets/Project/PrefabsR/HUD_Prefab/Relic.prefab`을 스킬 리스트 `contentRoot`에 생성하고 아이콘과 `remaining/max`를 표시한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않고, MSBuild 수준의 컴파일 검증만 수행한다.

메모:
- 전투 결과를 바꾸는 코드는 UI 클릭 함수 밖에 둔다.
- 씬 오브젝트 참조는 전투 경계에서만 찾고, 상태 변경 판단에는 `CharacterId`, `GridIndex`를 우선 사용한다.
- 네트워크 패키지나 멀티플레이 프레임워크 변경은 하지 않는다.
