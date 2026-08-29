# Skill SFX Database Migration Plan

## Goal

스킬 사운드의 파일 교체 위치를 VFX prefab 내부 AudioSource가 아니라 `Assets/DB/SoundDatabase.asset` 한 곳으로 모은다.

## Current Flow

- `SkillVfxDatabase`가 `SkillId -> BattleVfxEntry`를 가진다.
- `BattleUnitAnimator`가 플레이어 스킬 실행 시 `BattleVfxEntry.prefab`을 생성한다.
- 몬스터 스킬/액션은 몬스터 프리팹의 `monsterActionPresentations` 안에 `vfx`, `projectileVfx.missileSfx`, `projectileVfx.impactSfx`로 직접 연결되어 있다.
- `BattleVfxAudioUtility`가 생성된 VFX 내부 `AudioSource`를 찾아 `AudioManager.PlaySfxClip(AudioSource)`로 라우팅할 수 있다.
- 이 경우 재생 볼륨은 `AudioManager`를 타지만, 실제 AudioClip 참조는 VFX prefab 내부에 남는다.

## Target Flow

- `SoundDatabase`에 `skillSfxList`를 추가한다.
- `AudioManager`는 `skillSfxList`도 기존 SFX lookup dictionary에 등록한다.
- `BattleVfxEntry.sfx.sfxId`는 Skill SFX ID를 참조한다.
- `BattleVfxEntry.sfx.additionalSfx`는 한 VFX prefab 안에 기존 `AudioSource`가 여러 개 있던 케이스를 보존한다.
- `SoundData.loop`를 통해 기존 loop AudioSource를 DB 기반 routed AudioSource로 재생한다.
- 스킬용으로 마이그레이션한 VFX 내부 `AudioSource`는 런타임 재생 경로로 사용하지 않고 제거한다.
- 기존 스킬 외 VFX의 호환 경로는 유지하되, `playSfx = true`인 항목은 DB 사운드만 재생한다.
- Editor audit 도구가 `SkillVfxDatabase`, 캐릭터 `playerSkillPresentations`, 몬스터 `monsterActionPresentations`의 VFX prefab을 검사해 내부 `AudioSource` 잔존 여부와 `sfxId` 설정 상태를 리포트한다.

## Migration Policy

1. 신규 스킬 사운드는 `SoundDatabase.skillSfxList`에 등록한다.
2. 스킬 VFX는 `BattleVfxEntry.sfx.playSfx = true`, `sfxId = skill...` 형태로 연결한다.
3. DB로 연결한 스킬 VFX prefab 내부 `AudioSource`는 금지한다.
4. 기존 VFX 내부 `AudioSource`가 남아 있으면 audit 리포트와 테스트에서 잡는다.

## Migrated Existing Skill Sounds

기존에 AudioSource가 들어 있던 캐릭터 스킬/공격 VFX 9개와 몬스터 스킬/공격 VFX의 실제 재생 소리 8개를 `SoundDatabase.skillSfxList`로 옮겼다.

| Skill SFX ID | Source VFX | Preserved Clip | Volume | Pitch | Loop |
|---|---|---|---:|---:|---:|
| `skill.vfx.cha.01.attack.01` | `Vfx_Cha_01_attack_01.prefab` | `Vefects_SFX_Fire_Burst_01.wav` | 0.5 | 1.54 | 0 |
| `skill.vfx.cha.01.attack.02` | `Vfx_Cha_01_attack_02.prefab` | `SFX_Slash_Generic.wav` | 1 | 0.71 | 0 |
| `skill.vfx.cha.01.attack.03` | `Vfx_Cha_01_attack_03.prefab` | `Vefects_SFX_Slash_Classic.wav` | 1 | 0.85 | 0 |
| `skill.vfx.cha.02.attack.01` | `Vfx_Cha_02_attack_01.prefab` | `Vefects_SFX_Slash_Classic.wav` | 1 | 1 | 0 |
| `skill.vfx.cha.02.attack.02` | `Vfx_Cha_02_attack_02.prefab` | `Vefects_SFX_Slash_Classic.wav` | 1 | 1.22 | 0 |
| `skill.vfx.cha.02.attack.03` | `Vfx_Cha_02_attack_03.prefab` | `Vefects_SFX_Slash_Classic.wav` | 1 | 0.93 | 0 |
| `skill.vfx.cha.03.attack.01` | `Vfx_Cha_03_attack_01.prefab` | `SFX_Magic_Attack_Sound_Hit.wav` | 0.5 | 1 | 0 |
| `skill.vfx.cha.03.attack.02` | `Vfx_Cha_03_attack_02.prefab` | `SFX_Bomb_Launch.wav` | 1 | 1 | 0 |
| `skill.vfx.cha.03.attack.03` | `Vfx_Cha_03_attack_03.prefab` | `SFX_Magic_Attack_Sound_Hit.wav` | 0.5 | 1.64 | 0 |
| `skill.vfx.mon.e.02.attack.01` | `Vfx_Mon_E_02_attack_01.prefab` | `SFX_Vefects_Directional_Dust_01.wav` | 1 | 1 | 1 |
| `skill.vfx.mon.e.03.attack.03` | `Vfx_Mon_E_03_attack_03.prefab` | `SFX_Magic_Attack_Dark_Hit.wav` | 1 | 1 | 0 |
| `skill.vfx.mon.n.01.attack.01.impact` | `Vfx_Mon_N_01_attack_01.prefab` | `SFX_Magic_Attack_Dark_Hit.wav` | 0.5 | 1 | 0 |
| `skill.vfx.mon.n.01.attack.02.1.loop` | `Vfx_Mon_N_01_attack_02_1.prefab` | `SFX_Vefects_Hit_01.wav` | 1 | 1 | 1 |
| `skill.vfx.mon.n.01.attack.02.1.hit` | `Vfx_Mon_N_01_attack_02_1.prefab` | `SFX_Vefects_Fireball_01.wav` | 1 | 1 | 0 |
| `skill.vfx.mon.n.01.attack.02` | `Vfx_Mon_N_01_attack_02.prefab` | `SFX_Vefects_Directional_Dust_05_One_Shot_01.wav` | 1 | 1 | 0 |
| `skill.vfx.mon.n.04.attack.02` | `Vfx_Mon_N_04_attack_02.prefab` | `SFX_Slash_Dark.wav` | 0.5 | 1 | 0 |
| `skill.vfx.mon.n.05.attack.01` | `Vfx_Mon_N_05_attack_01.prefab` | `SFX_Bomb_Explosion.wav` | 1 | 1 | 1 |

연결 슬롯은 A/B/C BattlePrefab의 `playerSkillPresentations`와 `Cha_03_idle_0`의 C 캐릭터 프리젠테이션이다. 총 14개 슬롯이 DB SFX ID를 참조한다.

몬스터 연결 슬롯은 Ruke, Mort, Muck, Blob, Vespa, Sinder의 `monsterActionPresentations`이다. 총 7개 VFX 참조가 DB SFX ID를 가지며, Muck의 `Vfx_Mon_N_01_attack_02_1`은 `additionalSfx`로 두 번째 기존 AudioSource를 보존한다.

`Vfx_Mon_E_01_attack_02.prefab`의 AudioSource는 disabled 상태였고, `Vfx_Mon_N_01_attack_01_missile.prefab`의 AudioSource는 clip이 비어 있어 DB SFX를 만들지 않고 컴포넌트만 제거했다.

## Verification

- `SoundDatabase`와 `AudioManager` 단위 테스트로 Skill SFX 등록을 확인한다.
- `SoundIdDrawer` 테스트로 Skill SFX 드롭다운 범위를 확인한다.
- VFX audio utility 테스트로 embedded AudioSource가 라우팅되지 않고 제거되는지, loop DB SFX가 routed AudioSource로 생성되는지 확인한다.
- Editor audit 테스트로 캐릭터/몬스터 스킬 VFX prefab의 embedded AudioSource 감지와 프로젝트 마이그레이션 상태를 확인한다.
