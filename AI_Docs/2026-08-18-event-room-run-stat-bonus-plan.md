# 이벤트룸 런 지속 스탯 보너스 구현 계획

## 목표

`Event_01` 3번 선택지의 `RT002` 최대 체력 증가가 다음 배틀룸 입장 후에도 유지되게 한다.

## 작업 파일

- 수정: `Assets/Project/Scripts/Gameplay/Data/Runtime/CharacterRuntimeData.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceExecutionService.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Equipment/BattleEquipmentEffectService.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Data/Runtime/BattleRuntimeData.cs`
- 수정: `Assets/Project/Scripts/Core/SaveSystem.cs`
- 수정: `Assets/Tests/EditMode~/EventChoiceExecutionServiceTests.cs`
- 수정: `Assets/Tests/EditMode~/ReusableEquipmentEffectTests.cs`

## 구현 순서

1. `RT002`가 런 지속 최대 체력 보너스를 남기는 실패 테스트를 추가한다.
2. 전투 시작 계산이 런 지속 최대 체력 보너스를 보존하고 장비 보너스를 중복 누적하지 않는 실패 테스트를 추가한다.
3. `CharacterRuntimeData`에 `RunMaxHPBonus`, `RunMaxCostBonus`를 추가한다.
4. 이벤트 최대 체력/최대 코스트 변경 시 런 보너스 필드와 현재 표시 스탯을 함께 갱신한다.
5. 전투 시작 스탯 계산에서 `master + run bonus + equipment bonus` 구조를 사용한다.
6. 런 포기/세이브 정규화 경로에서 새 필드를 안전하게 초기화 또는 보정한다.
7. 관련 EditMode 테스트와 C# 프로젝트 빌드를 실행한다.

## 완료 기준

- `RT002` 선택지 테스트가 통과한다.
- `ApplyBattleStartEffects`가 이벤트 최대 체력 보너스를 덮어쓰지 않는다.
- 장비/룬 최대 체력 보너스가 전투 시작마다 누적되지 않는다.
- UI나 VFX가 전투 결과 계산에 관여하지 않는다.
