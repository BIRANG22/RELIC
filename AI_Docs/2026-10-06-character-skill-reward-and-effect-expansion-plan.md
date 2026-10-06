# Character Skill Reward and Effect Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CharacterID에 맞는 Core 스킬만 현재 파티의 보상·장착 후보로 허용하고, X 수식·효과 감소 규칙·실행 결과 기반 신규 효과를 구현한다.

**Architecture:** 캐릭터 호환성은 `SkillOwnershipPolicy`, X 해석은 `SkillNumericExpression`, 전투 후속 판정은 `SkillExecutionResult`로 분리한다. 보상 생성기는 기존 Core/희귀도/기본형/중복 규칙을 유지한 채 파티 호환성 조건만 추가하고, 효과는 레지스트리 기반 핸들러가 확정된 Command와 결과를 소비한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode/PlayMode tests, 기존 Excel `DataRowMapper`, `BattleRandom`

**Spec:** `AI_Docs/2026-10-06-character-skill-reward-and-effect-expansion-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 둔다.
- Unity 에디터가 열려 있다고 가정하고 batchmode 테스트를 실행하지 않는다.
- 현재 체크아웃과 현재 작업 공간에서 작업한다.
- 커밋, Push, PR, 브랜치·worktree 조작은 별도 사용자 승인 없이는 수행하지 않는다.
- 전투 핵심 로직은 `Command → State Change → Result/Event` 흐름을 유지한다.
- 전투 랜덤은 `BattleRandom`만 사용한다.
- UI/VFX/사운드는 전투 결과를 계산하지 않는다.
- 기존 Core 스킬 보상 분류·희귀도·기본형·중복 방지 규칙은 변경하지 않는다.

## Review Focus

- CharacterID가 빈 값이거나 공백/대소문자가 섞였을 때 잘못된 장착과 보상을 허용하지 않는지 확인한다.
- X의 단위 비용이 0, 음수, 현재 자원 초과일 때 예약 상태와 자원 소모가 일치하는지 확인한다.
- 한 공격에서 다단히트·다중 대상·동시 처치가 섞여도 타격 수와 처치 수가 중복 또는 누락되지 않는지 확인한다.
- `DecreaseOnTrigger`가 조건 불충족 또는 유효 대상 부재 시 감소하지 않는지 확인한다.
- 저장 복원 및 멀티플레이 스냅샷 후 `PermanentValueBonus`와 확정된 Command 값이 유지되는지 확인한다.

---

## File Map

### 새 파일

- `Assets/Project/Scripts/Gameplay/Data/Skill/SkillOwnershipPolicy.cs`: 캐릭터·파티 호환성 판정
- `Assets/Project/Scripts/Gameplay/Data/Skill/SkillNumericExpression.cs`: 고정 정수와 `숫자X` 파싱·평가
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/SkillImpactResult.cs`: 대상별 효과 결과
- `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/SkillExecutionResult.cs`: 스킬 전체 결과 집계
- 신규 효과별 클래스: 기존 `Effect/Effects` 하위의 Damage, Abnormal, Buff, Move, Special 폴더에 책임별 배치

### 주요 수정 파일

- `SkillMasterData.cs`, `SkillEffectEntry.cs`, `SkillEffectParser.cs`
- `EffectMasterData.cs`, `BattleStatusEffectService.cs`, `BattleEffectContext.cs`, `BattleEffectRegistry.cs`
- `SkillCostCalculator.cs`, `PlayerReservedCommand.cs`, `PlayerSkillReservationController.cs`, `PlayerActionPlanner.cs`
- `BattleActionRunner.cs`, `StrikeEffect.cs`, `PoisonTriggerEffect.cs`
- `SkillRuntimeData.cs`, `SkillRuntimeStore.cs`, `BattleRuntimeData.cs`, `SaveSystem.cs`
- `SkillRewardRoller.cs`, `BattleRewardResolver.cs`, `RelicChoiceAreaUI.cs`, `EventRoomController.cs`, `RestRoomShopService.cs`, `RestRoomShopPanel.cs`
- `BattleRewardEquipPanelUI.cs`, `SkillInventoryEquipService.cs`

## Task 1: CharacterID 로드와 공통 호환성 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillMasterData.cs`
- Create: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillOwnershipPolicy.cs`
- Test: `Assets/Tests/EditMode~/SkillOwnershipPolicyTests.cs`

**Interfaces:**
- Produces: `SkillMasterData.CharacterId`
- Produces: `SkillOwnershipPolicy.CanEquip(SkillMasterData, string)`
- Produces: `SkillOwnershipPolicy.CanRewardToParty(SkillMasterData, IEnumerable<string>)`
- Produces: `SkillOwnershipPolicy.GetCompatiblePartyCharacterIds(...)`

- [ ] **Step 1:** `SkillOwnershipPolicyTests`에 특정 ID 일치/불일치, `ALL`, 빈 값, 공백·대소문자 테스트를 작성한다.
- [ ] **Step 2:** Unity Test Runner에서 새 테스트가 필드·정책 부재로 실패하는지 확인한다.
- [ ] **Step 3:** `SkillMasterData.CharacterId`와 정책 클래스를 최소 구현한다.
- [ ] **Step 4:** Test Runner에서 `SkillOwnershipPolicyTests`가 모두 통과하는지 확인한다.
- [ ] **Step 5:** `GameData.xlsx`를 에디터에서 다시 로드해 `CharacterID` 값이 모델에 매핑되는지 확인한다.

## Task 2: Core 보상 후보에 파티 CharacterID 필터 추가

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillRewardRoller.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardResolver.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/RelicChoiceAreaUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomShopPanel.cs`
- Test: `Assets/Tests/EditMode~/CharacterSkillRewardCandidateTests.cs`

**Interfaces:**
- Consumes: Task 1의 `CanRewardToParty`
- Produces: 각 보상 경로가 파티 CharacterID 집합을 후보 생성 함수에 명시적으로 전달

- [ ] **Step 1:** Core가 아닌 스킬 제외, 파티 내 전용 Core 포함, 파티 외 전용 Core 제외, `ALL` Core 포함, 빈 ID 제외 테스트를 작성한다.
- [ ] **Step 2:** 기존 후보 함수가 파티를 보지 않아 실패하는지 확인한다.
- [ ] **Step 3:** `SkillRewardRoller.TryRoll` 및 후보 함수에 `IEnumerable<string> partyCharacterIds` 매개변수를 추가한다.
- [ ] **Step 4:** 전투·시작방·이벤트·상점 진입점에서 `PartyRuntimeStore`의 비어 있지 않은 CharacterID를 전달한다.
- [ ] **Step 5:** 기존 Core/희귀도/기본형/중복 방지 회귀 테스트와 새 테스트를 실행한다.

## Task 3: 장착 단계의 CharacterID 방어

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardEquipPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryEquipService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardEquipSelectionPolicy.cs`
- Test: `Assets/Tests/EditMode~/CharacterSkillEquipPolicyTests.cs`

**Interfaces:**
- Consumes: Task 1의 `CanEquip`, `GetCompatiblePartyCharacterIds`
- Produces: 불일치 캐릭터 장착 거부와 호환 캐릭터만 선택 가능한 보상 UI 모델

- [ ] **Step 1:** 전용 스킬 오장착 거부, `ALL` 허용, 빈 CharacterID 거부, 교체 실패 시 기존 스킬 보존 테스트를 작성한다.
- [ ] **Step 2:** 현재 장착 서비스가 캐릭터 호환성을 검사하지 않아 실패하는지 확인한다.
- [ ] **Step 3:** 실제 상태 변경 직전에 `CanEquip`을 검사하고 실패 시 인벤토리·장착 슬롯을 변경하지 않는다.
- [ ] **Step 4:** 보상 패널은 호환 캐릭터만 활성화하고 전용 스킬의 기본 대상도 호환 캐릭터로 제한한다.
- [ ] **Step 5:** 장착 정책 테스트와 기존 보상 장착 테스트를 실행한다.

## Task 4: X 수식 파싱과 예약 시 확정

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillNumericExpression.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillEffectEntry.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillEffectParser.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Skill/SkillCostCalculator.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerReservedCommand.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/PlayerActionPlanner.cs`
- Test: `Assets/Tests/EditMode~/SkillNumericExpressionTests.cs`
- Test: `Assets/Tests/EditMode~/PlayerSkillXReservationTests.cs`

**Interfaces:**
- Produces: `SkillNumericExpression.TryParse(string, out SkillNumericExpression)`
- Produces: `SkillNumericExpression.ResolveMaximumX(int availableResource)`
- Produces: `PlayerReservedCommand.ResolvedX`, `ResolvedResourceCost`, `ResolvedEffectValues`, `ResolvedEffectCounts`

- [ ] **Step 1:** 고정 수치, `3X`와 자원 11, X=0, 잘못된 식, 단위 비용 0 테스트를 작성한다.
- [ ] **Step 2:** 기존 정수 파서에서 `3X`가 처리되지 않아 실패하는지 확인한다.
- [ ] **Step 3:** 순수 수식 파서와 평가 API를 구현한다.
- [ ] **Step 4:** 예약 시 X와 효과 값을 Command에 저장하고 실행 시 원본 자원으로 재계산하지 않도록 연결한다.
- [ ] **Step 5:** 예약 후 자원을 변경해도 Command 결과가 유지되는 테스트를 통과시킨다.
- [ ] **Step 6:** 툴팁·타임라인 미리보기가 같은 확정값을 표시하는지 수동 확인한다.

## Task 5: 효과 수치 감소 규칙 일반화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Effect/EffectMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`
- Create or Modify: 효과 발동 수치 소비 공통 유틸리티
- Test: `Assets/Tests/EditMode~/EffectValueDecreaseRuleTests.cs`

**Interfaces:**
- Produces: `EffectValueDecreaseRule { None, Remove, Decrease, Maintain, DecreaseOnTrigger }`
- Produces: `TryConsumeOnTrigger(List<StatusEffectRuntimeData>, string effectId) : bool`

- [ ] **Step 1:** 기존 네 규칙과 성공한 발동에서만 감소하는 `DecreaseOnTrigger` 테스트를 작성한다.
- [ ] **Step 2:** 기존 `EndTurn` switch에 새 의미가 없어 실패하는지 확인한다.
- [ ] **Step 3:** 엑셀 호환을 유지하며 모델과 턴 종료 처리를 새 enum으로 전환한다.
- [ ] **Step 4:** 실제 효과 성공 지점에서만 `TryConsumeOnTrigger`를 호출하고 0이면 제거한다.
- [ ] **Step 5:** 조건 불충족·대상 부재·이미 사망한 대상에서 감소하지 않는 테스트를 실행한다.

## Task 6: 대상별/전체 실행 결과 모델

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/SkillImpactResult.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/SkillExecutionResult.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/BattleEffectContext.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/StrikeEffect.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Damage/PierceEffect.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/SkillExecutionResultTests.cs`

**Interfaces:**
- Produces: `SkillExecutionResult.RecordImpact(SkillImpactResult)`
- Produces: 집계 속성 `SuccessfulHitCount`, `DistinctHitTargetCount`, `KillCount`
- Consumes: `BattleEffectContext.ExecutionResult`

- [ ] **Step 1:** 다중 대상·다단히트·동시 처치·상태 스냅샷 집계 테스트를 작성한다.
- [ ] **Step 2:** 현재 직접 피해 핸들러가 결과를 남기지 않아 실패하는지 확인한다.
- [ ] **Step 3:** ID 기반 결과 DTO와 집계 함수를 구현한다.
- [ ] **Step 4:** `StrikeEffect`와 `PierceEffect`가 상태 변경 직전/직후 값을 기록하도록 연결한다.
- [ ] **Step 5:** 기존 피해·장비·흡혈·통계 처리가 중복 호출되지 않는지 회귀 테스트를 실행한다.

## Task 7: 직접 피해·조건부·이동 신규 효과

**Files:**
- Create: Damage 폴더의 `ArmorStrikeEffect.cs`, `RandomStrikeEffect.cs`, `StrikeByVulnerableEffect.cs`
- Create: Special 폴더의 `MissSelfDamageEffect.cs`, `DamageUpIfBleedingEffect.cs`, `RestoreManaPerHitTargetEffect.cs`, `StrikeCountByBuffEffect.cs`
- Create: Move 폴더의 `RushEffect.cs`
- Modify: `BattleEffectRegistry.cs`, `BattleActionRunner.cs`, 필요 시 `BattleDamageModifierUtility.cs`
- Test: `Assets/Tests/EditMode~/NewSkillDamageEffectTests.cs`
- Test: `Assets/Tests/PlayMode~/RushEffectTests.cs`

**Interfaces:**
- Consumes: Task 4의 확정 값, Task 6의 실행 결과
- Produces: 설계 문서의 신규 효과 계산 결과와 Rush 이동 결과

- [ ] **Step 1:** ArmorStrike, MissSelfDamage, 출혈 50%, 취약 계산, 타격당 마나, 버프 종류 타격 수 테스트를 작성한다.
- [ ] **Step 2:** RandomStrike의 CountRate, ValueRate, 중복 허용, 동일 seed 재현 테스트를 작성한다.
- [ ] **Step 3:** Rush의 빈 경로, 첫 충돌 앞 정지, 경계·이동 불가 셀 테스트를 작성한다.
- [ ] **Step 4:** 효과별 클래스를 구현하고 레지스트리에 등록한다.
- [ ] **Step 5:** `E_RandomStrike`가 `BattleRandom`만 사용하고 선택 RuntimeId 순서를 결과에 기록하도록 구현한다.
- [ ] **Step 6:** Rush는 피해 없이 도착·충돌 결과만 저장하도록 구현한다.
- [ ] **Step 7:** EditMode/PlayMode 테스트와 기존 이동·피해 회귀 테스트를 실행한다.

## Task 8: 상태 기반 신규 효과와 발동 감소

**Files:**
- Create: Abnormal 폴더의 `BondageEffect.cs`, `PoisonByTargetPoisonEffect.cs`
- Modify or Move: `Assets/Project/Scripts/PoisonTriggerEffect.cs`
- Modify: `BattleEffectRegistry.cs`, 이동 예약/실행 검증 지점
- Test: `Assets/Tests/EditMode~/NewSkillStatusEffectTests.cs`
- Test: `Assets/Tests/PlayMode~/BondageMovementTests.cs`

**Interfaces:**
- Consumes: Task 5의 `TryConsumeOnTrigger`
- Produces: `E_TriggerPoison` 정식 ID와 `E_PoisonTrigger` 호환 별칭

- [ ] **Step 1:** Bondage 마지막 수치가 현재 이동을 막고 다음 이동을 허용하는 테스트를 작성한다.
- [ ] **Step 2:** `floor(poison / ValueRate)`, 0 이하 ValueRate, 즉시 중독 피해 후 1 감소 테스트를 작성한다.
- [ ] **Step 3:** 예약과 실행 양쪽에서 Bondage를 검사하되 실행 판정을 최종 권위로 구현한다.
- [ ] **Step 4:** PoisonByTargetPoison과 TriggerPoison을 구현하고 기존 ID를 같은 핸들러에 매핑한다.
- [ ] **Step 5:** 성공 시에만 DecreaseOnTrigger가 소비되는지 검증한다.

## Task 9: 처치 기반 원정 영구 성장

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/SkillRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/SkillRuntimeStore.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/BattleRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Core/SaveSystem.cs`
- Create: Special 폴더의 `ValueUpOnKillEffect.cs`
- Modify: 직접 피해 값 계산과 원정 초기화 경로
- Test: `Assets/Tests/EditMode~/SkillPermanentValueBonusTests.cs`

**Interfaces:**
- Produces: `SkillRuntimeData.PermanentValueBonus`
- Produces: `AddPermanentValueBonus(string characterId, string skillId, int amount)`
- Consumes: Task 6의 `SkillExecutionResult.KillCount`

- [ ] **Step 1:** 처치당 ValueRate, 다중 처치, 현재 공격 미반영, 다음 사용 반영 테스트를 작성한다.
- [ ] **Step 2:** 저장·복원과 원정 초기화 테스트를 작성한다.
- [ ] **Step 3:** `CharacterId + SkillId` 런타임 데이터에 보너스를 누적한다.
- [ ] **Step 4:** 첫 번째 직접 피해 효과의 기본값에 보너스를 더하고 공격 완료 후 KillCount만큼 누적한다.
- [ ] **Step 5:** 저장 복원·런타임 복사·원정 초기화 경로에 필드를 반영한다.
- [ ] **Step 6:** 관련 테스트를 실행하고 MasterData가 변경되지 않는지 확인한다.

## Task 10: 통합 검증과 데이터 진단

**Files:**
- Test: `Assets/Tests/EditMode~/CharacterSkillRewardAndEffectsIntegrationTests.cs`
- Modify only if needed: 데이터 로드 진단 코드

**Interfaces:**
- Consumes: Task 1~9의 공개 인터페이스
- Produces: 전체 요구사항 통합 회귀 증거

- [ ] **Step 1:** 현재 파티 전용 Core 보상부터 올바른 캐릭터 장착까지 통합 테스트를 작성한다.
- [ ] **Step 2:** X 예약, 다단히트 결과, 후속 효과, 영구 증가까지 이어지는 통합 테스트를 작성한다.
- [ ] **Step 3:** 빈 CharacterID, 잘못된 X, 0 이하 중독 나눗수 데이터가 경고되고 실행에서 제외되는지 검증한다.
- [ ] **Step 4:** Unity Test Runner에서 관련 EditMode/PlayMode 전체 테스트를 실행한다.
- [ ] **Step 5:** `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드하고 새 오류가 없는지 확인한다.
- [ ] **Step 6:** Battle 씬에서 파티 구성별 보상, 장착, 신규 효과를 수동 검증한다.
- [ ] **Step 7:** `git diff --check`와 변경 파일 목록을 확인하고 기존 사용자 변경과 섞이지 않았는지 검토한다.

## 실행 순서와 승인 경계

Task 1부터 Task 10까지 순서대로 진행한다. 각 Task는 테스트 실패 확인 → 최소 구현 → 관련 테스트 통과 순서로 수행한다. 승인된 구현 범위 안에서는 단계별 재승인을 요청하지 않는다.

커밋, Push, PR, 브랜치 생성·전환, worktree 생성·삭제는 이 계획에 포함하지 않으며, 사용자가 별도로 승인한 경우에만 수행한다.

