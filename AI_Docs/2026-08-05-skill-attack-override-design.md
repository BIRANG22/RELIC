# Skill Attack Override 설계

## 목표

아군 캐릭터가 공격 스킬을 사용할 때 기존 `Attack1~3` 랜덤 선택을 기본값으로 유지하되, 특정 `CharacterId + SkillId` 조합만 원하는 공격 슬롯으로 고정할 수 있게 한다.

## 권장 구조

- 새 ScriptableObject DB: `SkillAttackOverrideDatabase`
- 엔트리 키: `characterId + skillId`
- 엔트리 값: `Attack1`, `Attack2`, `Attack3`
- 조회 실패 시 기존 랜덤 공격 선택을 그대로 사용한다.

## 적용 흐름

1. `BattleUnitAnimator.PlaySkillAction(SkillMasterData skillData)`가 공격 스킬을 받는다.
2. 현재 애니메이터 소유 유닛의 `CharacterId`를 확인한다.
3. `DataManager.SkillAttackOverrideDatabase`에서 `CharacterId + SkillId` 매핑을 조회한다.
4. 매핑이 있으면 지정된 `Attack1~3` 프레젠테이션을 재생한다.
5. 매핑이 없거나 지정 슬롯이 비어 있으면 기존 랜덤 선택으로 되돌아간다.

## 범위

- 전투 결과, 데미지, 비용, 타겟 판정은 변경하지 않는다.
- VFX/애니메이션 선택만 바꾸며, 기존 `BattleUnitActionPresentation`과 `BattleVfxEntry` 구조를 재사용한다.
- 몬스터 스킬 행동 매핑은 이번 범위에 포함하지 않는다.

## 검증 계획

- DB가 `characterId + skillId`로 공격 슬롯을 반환하는지 검증한다.
- `BattleUnitAnimator`가 매핑된 공격 스킬에 지정 슬롯 VFX를 재생하는지 검증한다.
- 매핑이 없는 공격 스킬은 기존 랜덤 경로를 유지해 등록된 공격 VFX 중 하나를 재생하는지 검증한다.

## 멀티플레이 경계

이 기능은 결과 계산 이후의 로컬 연출 선택이다. 상태 변경은 `SkillId`, `CharacterId` 기반의 기존 명령/런타임 데이터를 읽기만 하며, 전투 결과를 변경하지 않는다.
