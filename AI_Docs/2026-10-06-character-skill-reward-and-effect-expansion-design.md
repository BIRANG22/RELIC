# 캐릭터 전용 스킬 보상 및 신규 효과 확장 설계

## 목적

`GameData.xlsx`의 `SkillMaster.CharacterID`를 런타임에 반영하여 스킬을 허용된 캐릭터에게만 장착하고, 현재 원정 파티에 포함된 캐릭터의 Core 스킬만 보상 후보로 사용한다. 동시에 X 기반 자원 소모, 효과 수치 감소 규칙, 직접 피해 결과 기록과 신규 효과 12종을 멀티플레이 확장 가능한 구조로 추가한다.

## 범위

- `SkillMaster.CharacterID` 로드와 캐릭터 호환성 판정
- 기존 Core 스킬 보상 조건에 현재 파티 CharacterID 필터 추가
- 보상 장착 및 스킬 인벤토리 장착 시 최종 호환성 검증
- X 수식 파싱·예약·실행
- `EffectMaster.EndTurn`을 일반화한 수치 감소 규칙
- 직접 피해 결과를 후속 효과가 사용할 수 있는 실행 결과 모델
- 신규 효과 구현 및 기존 `E_PoisonTrigger` 호환
- 원정 동안 유지되는 스킬 영구 증가값
- EditMode/PlayMode 테스트와 컴파일 검증

다음은 범위에서 제외한다.

- Core 이외 카테고리를 일반 랜덤 보상으로 변경하는 작업
- 네트워크 프레임워크 신규 도입
- UI/VFX/사운드의 전투 결과 계산 참여
- 기존 희귀도, 기본/강화형 짝, 중복 보상 차단 규칙 변경
- 아트 리소스나 씬 배치 변경

## 현재 구조와 문제

- `SkillMasterData`에는 `CharacterID` 필드가 없다.
- `SkillRewardRoller`는 Core, 희귀도, 기본형 여부는 검사하지만 캐릭터 호환성을 검사하지 않는다.
- 전투 보상, 시작방, 이벤트방, 휴식방 상점이 각각 후보 목록을 구성한다.
- 보상 장착 및 인벤토리 장착 단계에는 캐릭터 전용 스킬을 막는 공통 정책이 없다.
- `ResourceCostValue`와 효과 값은 고정 정수 전제로 계산되어 `3X` 같은 표현을 처리하지 못한다.
- 직접 피해 효과가 즉시 상태를 변경하며, 명중 수·대상 수·처치 수를 후속 효과가 읽을 공통 결과가 없다.
- `EndTurn`은 턴 종료 처리만 표현하므로 실제 효과 발동 시 수치를 감소시키는 규칙을 나타내지 못한다.

## 1. CharacterID 데이터와 호환성 정책

`SkillMasterData`에 엑셀 열과 동일하게 매핑되는 `CharacterId`를 추가한다.

### 값 규칙

| CharacterID | 의미 |
| --- | --- |
| 특정 캐릭터 ID | 해당 캐릭터만 장착 가능 |
| `ALL` | 모든 캐릭터 장착 가능 |
| 빈 값 | 잘못된 데이터이며 보상·장착 불가 |

비교 시 앞뒤 공백을 제거하고 대소문자를 구분하지 않는다. 현재 요구 범위에서는 단일 CharacterID와 `ALL`만 정식 데이터로 취급한다.

### 공통 정책

`SkillOwnershipPolicy`가 모든 판정을 담당한다.

```csharp
bool CanEquip(SkillMasterData skill, string characterId)
bool CanRewardToParty(SkillMasterData skill, IEnumerable<string> partyCharacterIds)
IReadOnlyList<string> GetCompatiblePartyCharacterIds(
    SkillMasterData skill,
    IEnumerable<string> partyCharacterIds)
```

보상 후보와 장착 UI가 같은 정책을 사용한다. 외부 보상이나 이전 캐시를 통해 잘못된 스킬이 들어와도 실제 장착 단계에서 다시 거부한다.

## 2. Core 보상 규칙 유지와 파티 필터 추가

기존 Core 보상 조건은 변경하지 않는다.

```text
전체 SkillMaster
→ Category == Core
→ 보상 가능한 희귀도
→ 기본 단계 스킬
→ 이미 보유·대기 중인 스킬 및 대응 강화형 제외
→ CharacterID == ALL 또는 현재 파티 CharacterID와 일치
→ BattleRandom으로 최종 선택
```

적용 경로:

- 전투 종료 스킬 보상
- 시작방 Core 스킬 선택 보상
- 이벤트방 랜덤 스킬 보상
- 휴식방 Core 스킬 상점
- 고정/외부 스킬 보상의 장착 대상 선택
- 전투 중 스킬 인벤토리 장착

현재 파티는 `PartyRuntimeStore.Slots`의 비어 있지 않은 CharacterID 집합으로 만든다. 후보 계산 함수에는 이 집합을 명시적으로 전달하여 전역 mutable static 상태에 의존하지 않는다.

## 3. X 수식

X는 현재 사용할 수 있는 자원으로 실행 가능한 최대 배수다.

```text
X = floor(현재 사용 가능 자원 / 단위 자원 소모량)
총 소모량 = 단위 자원 소모량 × X
```

예: 비용 `3X`, 현재 마나 11이면 X는 3, 실제 소모량은 9, 남은 마나는 2다.

### 규칙

- 별도 최대 X 없이 가능한 최대 X를 모두 사용한다.
- X가 0이면 예약할 수 없다.
- 단위 비용이 0 이하인 X 식은 유효하지 않다.
- 고정 숫자와 `숫자X`를 모두 지원한다.
- 예약 시점에 X, 실제 총 비용, X가 적용된 효과 값과 횟수를 확정한다.
- 실행 시점에는 자원 상태로 X를 다시 계산하지 않는다.
- 툴팁과 예약 미리보기도 같은 해석기를 사용한다.

`PlayerReservedCommand`에 다음 확정값을 저장한다.

```text
ResolvedX
ResolvedResourceCost
ResolvedEffectValues
ResolvedEffectCounts
```

## 4. 효과 수치 감소 규칙

기존 `EndTurn` 의미를 `EffectValueDecreaseRule`로 일반화한다. 엑셀 열이 `EndTurn`을 유지하는 동안에도 로더에서 새 enum으로 매핑하며, 향후 열 이름을 변경하면 기존 이름을 호환 별칭으로 지원한다.

| 데이터 값 | 동작 |
| --- | --- |
| `None` 또는 `0` | 수치를 사용하지 않음 |
| `Decrease` | 턴 종료 시 수치 1 감소 |
| `Remove` | 턴 종료 시 효과 제거 |
| `Maintain` | 턴 종료 시 수치 유지 |
| `DecreaseOnTrigger` | 효과 기능이 실제로 성공할 때 수치 1 감소 |

`DecreaseOnTrigger`는 조건 불충족, 유효 대상 부재, 사망한 대상 등으로 기능이 실행되지 않으면 감소하지 않는다. 감소 후 수치가 0이면 상태 효과를 제거한다. 상태 변경은 전투 로직이 수행하고 UI/VFX는 결과 이벤트만 재생한다.

## 5. 스킬 실행 결과 모델

직접 효과와 후속 효과를 다음 순서로 실행한다.

```text
PlayerReservedCommand
→ 직접 피해·이동·상태 변경
→ 대상별 SkillImpactResult 기록
→ SkillExecutionResult 집계
→ 조건부·후속 효과 실행
→ Result/Event 발행
```

### 대상별 결과

`SkillImpactResult`는 다음 안정적인 데이터를 가진다.

```text
TargetRuntimeId
TargetGridIndex
Attempted
Hit
DamageAmount
Killed
HadBleedingBeforeHit
VulnerableValueBeforeHit
PoisonValueBeforeEffect
```

### 전체 실행 결과

`SkillExecutionResult`는 다음을 집계한다.

```text
CasterCharacterId
SkillId
AttemptedHitCount
SuccessfulHitCount
DistinctHitTargetCount
KillCount
CasterBeneficialEffectTypeCount
RushDestinationGridIndex
RushCollisionRuntimeId
RushCollisionGridIndex
Impacts
```

`E_Strike`, `E_Pierce` 및 신규 직접 피해 효과가 결과를 기록한다. 후속 효과는 Scene Object를 다시 검색해 판정하지 않고 이 결과를 읽는다.

## 6. 신규 효과

| 효과 ID | 동작 |
| --- | --- |
| `E_ArmorStrike` | 시전자 현재 방어도 × `ValueRate` 피해 |
| `E_MissSelfDamage` | 선택 범위 내 유효한 적이 0명이면 자신에게 `ValueRate` 피해 |
| `E_ValueUpOnKill` | 해당 스킬로 처치한 적 1명당 다음 사용부터 영구 수치 `ValueRate` 증가 |
| `E_DamageUpIfBleeding` | 대상이 출혈을 보유하면 최종 피해 50% 증가 |
| `E_Rush` | `ValueRate`칸 전진, 첫 충돌 앞에서 정지하고 이동·충돌 결과 저장 |
| `E_Bondage` | 이동 시도 1회를 막고 `DecreaseOnTrigger`이면 수치 1 감소 |
| `E_PoisonByTargetPoison` | `floor(대상 중독 수치 / ValueRate)`만큼 중독 부여 |
| `E_TriggerPoison` | 현재 중독 수치만큼 즉시 피해 후 중독 수치 1 감소 |
| `E_RestoreManaPerHitTarget` | 성공한 총 타격 수 × `ValueRate`만큼 마나 회복 |
| `E_RandomStrike` | `CountRate`회 독립 추첨, 매회 `ValueRate` 피해, 중복 대상 허용 |
| `E_StrikeByVulnerable` | `ValueRate + 취약 수치 × CountRate` 피해 |
| `E_StrikeCountByBuff` | 시전자가 가진 서로 다른 Beneficial EffectId 종류 수만큼 타격 횟수 증가 |

### 세부 규칙

- `E_RandomStrike`는 `BattleRandom`만 사용하며 seed 또는 결정된 대상 RuntimeId 순서를 결과에 저장한다.
- `E_RestoreManaPerHitTarget`은 대상 2명에게 각각 2회 적중하면 4회로 계산한다.
- `E_StrikeCountByBuff`는 같은 버프의 스택 수를 무시하고 서로 다른 EffectId만 센다.
- `E_Rush`는 피해를 계산하지 않고 실제 도착 GridIndex와 첫 충돌 대상만 기록한다. 후속 효과가 이를 소비한다.
- `E_Bondage` 수치가 1이면 해당 이동은 막고 수치를 제거하며, 다음 이동부터 허용한다.
- `E_TriggerPoison`의 정식 ID를 사용하고 기존 `E_PoisonTrigger`는 같은 핸들러를 가리키는 호환 별칭으로 유지한다.

## 7. E_ValueUpOnKill 영구 성장

`SkillMasterData` 원본은 변경하지 않는다. `SkillRuntimeData`에 원정용 `PermanentValueBonus`를 추가하고 `CharacterId + SkillId`로 저장한다.

```text
EffectIds: E_Strike;E_ValueUpOnKill
ValueRate: 10;2
```

적 1명을 처치하면 현재 공격 종료 후 보너스 2를 누적하고 다음 사용부터 첫 번째 직접 피해 효과가 12가 된다. 여러 적을 처치하면 `KillCount × E_ValueUpOnKill.ValueRate`를 더한다.

직접 피해 효과 범위:

- `E_Strike`
- `E_ArmorStrike`
- `E_RandomStrike`
- `E_StrikeByVulnerable`

증가값은 현재 원정 전체에서 유지하고 원정 초기화 시 제거한다. 저장 데이터와 멀티플레이 스냅샷 복사 경로에도 포함한다.

## 8. 오류 처리와 데이터 검증

- 빈 CharacterID는 로드하되 보상·장착 후보에서 제외하고 경고한다.
- 알 수 없는 CharacterID는 현재 파티와 일치하지 않으므로 후보에서 제외한다.
- X 수식이 잘못됐거나 단위 비용이 0 이하이면 예약을 거부하고 경고한다.
- `E_PoisonByTargetPoison`의 ValueRate가 0 이하이면 효과를 실행하지 않고 경고한다.
- 유효 대상이 없는 `E_RandomStrike`는 피해 없이 종료하며 후속 명중 효과도 발생시키지 않는다.
- 등록되지 않은 EffectId는 기존 레지스트리 경고 정책을 유지한다.

## 9. 테스트 전략

테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 작성한다. Unity batchmode는 사용하지 않는다.

- 데이터: CharacterID, ALL, 빈 값, X 수식, 감소 규칙 로딩
- 보상: Core 조건 보존, 파티 필터, ALL, 빈 값, 중복/강화형 제외
- 장착: 허용 캐릭터, 불일치 캐릭터, 외부 보상 방어
- 실행: 예약 시 X 확정, 실행 시 재계산 금지
- 결과: 타격, 고유 대상, 처치, 상태 스냅샷 집계
- 효과: 각 신규 효과의 확정 계산식과 경계값
- 랜덤: 같은 seed에서 같은 `E_RandomStrike` 대상 순서
- 영구 성장: 처치 수 누적, 다음 사용부터 반영, 원정 초기화
- 감소 규칙: 성공한 실제 발동에서만 `DecreaseOnTrigger` 감소

## 10. 멀티플레이 영향

전투 결과에 영향을 주는 입력과 결과는 `CharacterId`, `SkillId`, `RuntimeId`, `GridIndex`, 예약 시 확정값, `BattleRandom` seed 또는 선택 결과로 표현한다. 네트워크 프레임워크를 효과 클래스에 직접 연결하지 않는다.

```text
Command → State Change → SkillExecutionResult/Event
```

UI, 카메라, VFX, 사운드는 계산에 참여하지 않고 결과만 재생한다.

