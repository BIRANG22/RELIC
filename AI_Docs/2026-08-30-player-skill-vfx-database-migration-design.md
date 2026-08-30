# Player Skill VFX Database Migration Design

## Goal

`BattleUnitAnimator.playerSkillPresentations`에 직접 연결된 플레이어 스킬 VFX를 `SkillVfxDatabase`로 이전한다.

## Current State

- A/B/C 전투 프리팹의 `playerSkillPresentations.attack1~3.vfx.prefab`에 캐릭터 스킬 VFX가 직접 연결되어 있다.
- C 전투 프리팹과 `Cha_03_idle_0`에는 `playerSkillPresentations.skill.vfx.prefab`에도 `Vfx_Cha_03_attack_01`이 중복 연결되어 있다.
- `SkillVfxDatabase.asset`에는 `S_Ability_11`, `S_Public_17`, `S_Public_19`만 등록되어 있다.
- `S_Public_17`, `S_Public_19`는 현재 런타임 데이터에서 각각 `S_Core_77`, `S_Core_79`에 대응한다.

## Recommended Design

- `playerSkillPresentations`는 애니메이션 stateName과 행동 슬롯 선택만 담당한다.
- 플레이어 스킬 VFX는 `SkillVfxDatabase`의 `SkillId -> BattleVfxEntry` 매핑으로만 재생한다.
- 캐릭터 전용 Ability는 기본/강화 쌍 모두 같은 VFX를 사용한다.
  - `S_Ability_01`, `S_Ability_02` -> `Vfx_Cha_01_attack_01`
  - `S_Ability_03`, `S_Ability_04` -> `Vfx_Cha_01_attack_03`
  - `S_Ability_05`, `S_Ability_06` -> `Vfx_Cha_02_attack_01`
  - `S_Ability_07`, `S_Ability_08` -> `Vfx_Cha_02_attack_03`
  - `S_Ability_09`, `S_Ability_10` -> `Vfx_Cha_03_attack_01`
  - `S_Ability_11`, `S_Ability_12` -> existing flash explosion VFX
- Char_04/05가 현재 C 전투 프리팹을 공유하므로 연결된 C 슬롯 VFX도 현재 실제 스킬 ID에 보수적으로 등록한다.
  - `S_Ability_13`, `S_Ability_14` -> `Vfx_Cha_03_attack_02`
  - `S_Ability_17`, `S_Ability_18` -> `Vfx_Cha_03_attack_03`
- 캐릭터 기본 공용 슬롯으로 보이는 나머지 직접 연결 VFX도 현재 Core ID에 등록한다.
  - `S_Core_63` -> `Vfx_Cha_01_attack_02`
  - `S_Core_67` -> `Vfx_Cha_02_attack_02`
- 기존 Public ID 항목은 호환용으로 유지하고, 현재 Core ID 항목을 추가한다.
- `SkillAttackOverrideDatabase`의 `S_Ability_03 ` 뒤 공백은 정리한다.

## Verification

- EditMode 에셋 테스트로 `SkillVfxDatabase.asset`에 필요한 SkillId와 VFX GUID가 들어 있는지 검사한다.
- EditMode 에셋 테스트로 플레이어 전투 프리팹의 `playerSkillPresentations` VFX 참조가 비어 있는지 검사한다.
- C 전투 프리팹과 `Cha_03_idle_0`의 중복 `skill.vfx.prefab`도 비어 있는지 검사한다.

## Multiplayer Boundary

이번 변경은 결과 계산 이후의 로컬 연출 데이터 위치만 바꾼다. 전투 판정, 데미지, 타겟, 랜덤, 상태 변경 흐름은 수정하지 않는다.
