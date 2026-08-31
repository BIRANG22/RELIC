# Skill Projectile / Tile VFX Design & Plan

## 목적

`SkillVfxDatabase`의 스킬별 연출을 시전자 VFX와 투사체 VFX로 분리한다. 선택 타일 즉시 연출은 몬스터와 동일하게 투사체의 Impact-only 설정으로 표현한다.

## 조사 결과

- `SkillVfxEntry`는 현재 `SkillId`와 단일 `BattleVfxEntry Vfx`만 가진다.
- `BattleUnitAnimator`에는 몬스터용 `BattleProjectileVfxEntry` 재생 기능이 이미 있다.
- 플레이어 스킬의 선택 타일은 동기화되는 명령 데이터인 `SelectedGridIndex`로 전달된다.
- 기존 타일 배치는 `BattleVfxEntry.spawnAnchor` 분기로 처리되어 시전자 VFX와 타일 VFX의 역할이 한 필드에 섞여 있다.

## 권장 설계

- `SkillVfxEntry`에 `Vfx`, `ProjectileVfx`를 둔다.
- `Vfx`는 항상 시전자 기준으로 재생한다.
- 미사일과 Impact가 모두 있으면 선택 타일까지 이동을 완료한 뒤 Impact를 재생한다.
- 미사일 없이 Impact만 있으면 스킬 모션 시작과 함께 선택 타일에 Impact를 재생한다.
- 선택 타일이 없거나 유효하지 않으면 투사체와 Impact VFX만 생략한다.
- `BattleVfxSpawnAnchor`와 `BattleVfxEntry.spawnAnchor`를 제거한다.
- 기존 `S_Ability_11`, `S_Ability_12`의 선택 타일 VFX를 `ProjectileVfx.impactPrefab`으로 이전한다.

## 구현 계획

1. EditMode 테스트를 새 데이터 계약과 Impact 재생 순서에 맞게 먼저 변경하고 실패를 확인한다.
2. `SkillVfxDatabase`가 전체 `SkillVfxEntry`를 조회하면서 기존 `TryGetVfx` 호환 API를 유지하도록 변경한다.
3. `BattleUnitAnimator`의 플레이어 스킬 대상 VFX 코루틴에서 기존 몬스터 투사체 재생 코드를 재사용한다.
4. `BattleActionRunner`가 스킬 모션 직후 대상 VFX 코루틴을 기다린 다음 효과를 적용하도록 연결한다.
5. `SkillVfxDatabase.asset`의 기존 선택 타일 항목을 Impact-only `ProjectileVfx`로 이전한다.
6. 컴파일, EditMode 테스트 가능 범위, 정적 검색, `git diff --check`로 검증한다.

## 멀티플레이 경계

전투 판정이나 상태를 추가하지 않고 기존 명령의 `SkillId`와 `SelectedGridIndex`만 읽어 로컬 연출 순서를 결정한다. 피해, 상태 변경, 랜덤, 네트워크 프레임워크에는 영향을 주지 않는다.
