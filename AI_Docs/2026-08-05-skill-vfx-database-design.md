# 스킬 고유 VFX DB 설계

## 목적

스킬 사용 시 기존 유닛 행동 VFX와 별개로 `SkillId`에 대응하는 고유 VFX를 추가 재생한다. 모든 스킬이 고유 VFX를 갖는 것은 아니므로, DB에 등록된 스킬만 재생한다.

## 조사 결과

- 플레이어 스킬 행동 연출은 `BattleUnitAnimator.PlaySkillAction(SkillMasterData)`와 `PlaySkillAction(SkillMasterData, int hitIndex)`에서 처리된다.
- 기존 시전자 행동 VFX는 `BattleUnitActionPresentation.vfx`를 통해 재생된다.
- 피격, 회복, 상태이상 VFX는 별도 메서드와 `BattleStatusVfxSet`으로 분리되어 있다.
- 데미지 스킬은 히트 수만큼 `PlaySkillAction(skillData, hitIndex)`가 호출될 수 있다.

## 권장 구조

- `SkillVfxDatabase`를 ScriptableObject로 추가한다.
- 각 엔트리는 `SkillId`와 `BattleVfxEntry`만 가진다.
- `BattleUnitAnimator`는 스킬 행동 재생 시 DB를 조회해 스킬 고유 VFX를 추가로 재생한다.
- `PlaySkillAction(skillData, hitIndex)`에서는 `hitIndex <= 0`일 때만 스킬 고유 VFX를 재생한다.

## 적용 예시

- `S_Ability_11` -> `Vfx_SpriteAni_flash_explosion`

## 멀티플레이 경계

이 기능은 전투 결과를 계산하지 않고 스킬 결과 이벤트에 따른 연출만 추가한다. 전투 판정, 피해량, 상태 변경, 랜덤 처리에는 관여하지 않는다.
