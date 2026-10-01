# 공용 스킬 공격 연출 오버라이드 설계

## 목표

`SkillAttackOverrideDatabase`에서 캐릭터 ID와 무관하게 특정 스킬이 모든 캐릭터의 동일한 공격 슬롯을 사용하도록 설정한다.

## 데이터 구조

- `SkillAttackOverrideEntry`에 `ApplyToAllCharacters` 체크박스를 추가한다.
- 체크하면 `CharacterId`를 사용하지 않고 `SkillId`만으로 공용 설정을 등록한다.
- 체크하지 않은 기존 엔트리는 `CharacterId + SkillId` 방식으로 유지한다.

## 조회 우선순위

1. 실제 `CharacterId + SkillId`에 해당하는 캐릭터 전용 설정을 조회한다.
2. 전용 설정이 없으면 `ApplyToAllCharacters`가 체크된 동일 `SkillId` 설정을 조회한다.
3. 둘 다 없으면 기존 기본 공격 연출 선택 경로를 사용한다.

`AttackSlot`과 다단 공격용 `RepeatAttackSlots`에 같은 우선순위를 적용한다. 같은 스킬의 공용 설정이 여러 개면 경고하고 첫 엔트리를 사용한다.

## 인스펙터

`ApplyToAllCharacters`가 체크된 엔트리는 혼동을 막기 위해 `CharacterId` 입력란을 비활성화한다.

## 검증

- 공용 설정이 임의 캐릭터에 적용되는지 확인한다.
- 캐릭터 전용 설정이 공용 설정보다 우선하는지 확인한다.
- 기존 캐릭터 전용 설정과 미등록 조회 동작이 유지되는지 확인한다.

## 멀티플레이 경계

전투 결과나 상태는 변경하지 않는다. 안정적인 `CharacterId`, `SkillId`를 읽어 로컬 애니메이션/VFX 프레젠테이션 슬롯만 선택한다.
