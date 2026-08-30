# VFX Sound Database Design

## 목표

- 스킬 VFX는 더 이상 사운드 ID를 들고 있지 않는다.
- `SoundDatabase`가 VFX 프리팹과 사운드 파일 큐를 직접 연결한다.
- `SkillVfxDatabase`는 스킬 ID별 VFX 위치, 렌더링, 스폰 기준만 관리한다.
- `BattleUnitAnimator`는 스킬별 사운드가 아니라 캐릭터 행동/애니메이션/프리젠테이션만 관리한다.
- 플레이어 VFX 사운드와 몬스터 VFX 사운드는 `SoundDatabase` 안에서 별도 리스트로 나눈다.

## 권장 구조

`SoundDatabase`는 기존 `Bgm`, `Sfx` ID 조회를 유지하고, 스킬 VFX 사운드는 다음 두 리스트로 관리한다.

- `playerSkillVfxSfxList`: 플레이어/캐릭터 스킬 VFX 프리팹별 사운드 큐
- `monsterSkillVfxSfxList`: 몬스터 스킬 VFX 프리팹별 사운드 큐

각 VFX 사운드 항목은 `GameObject vfxPrefab`과 `List<VfxSoundCue>`를 가진다. 큐는 `AudioClip`, `delay`, `volume`, `pitch`, `loop`, `randomPitch`만 보유한다. 사운드 ID, alias, SkillSfx 카테고리는 사용하지 않는다.

## 재생 흐름

1. 전투 결과 이벤트가 스킬/행동 프리젠테이션을 요청한다.
2. `SkillVfxDatabase` 또는 `BattleUnitAnimator` 프리젠테이션이 어떤 VFX를 어디에 띄울지만 결정한다.
3. VFX 인스턴스가 생성되면 `BattleVfxAudioUtility`가 원본 VFX 프리팹을 `SoundDatabase`에 조회한다.
4. 매핑된 `VfxSoundCue`를 `AudioManager`를 통해 재생한다.
5. VFX 안에 남아 있는 기존 `AudioSource`는 전투 믹서/볼륨 정책을 우회하지 않도록 제거한다.

## 마이그레이션 범위

- `BattleVfxEntry.sfx`, `BattleVfxSfxEntry`, `BattleVfxAdditionalSfxEntry` 제거
- `BattleProjectileVfxEntry.missileSfx`, `impactSfx` 제거
- `SoundCategory.SkillSfx`, `skillSfxList`, `SkillSfxEntries` 제거
- 기존 `skillSfxList` 사운드는 VFX 프리팹 직접 매핑으로 이전
- `SkillVfxDatabase.asset`과 프리팹/씬의 옛 SFX 직렬화 필드는 제거 대상

## 멀티플레이 경계

이 변경은 전투 결과를 계산하지 않는다. 사운드와 VFX는 결과 이벤트를 재생하는 프리젠테이션 계층에 남으며, 전투 핵심 상태 변경이나 판정 흐름에는 영향을 주지 않는다.
