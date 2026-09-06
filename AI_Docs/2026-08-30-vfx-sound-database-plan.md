# VFX Sound Database Implementation Plan

## 1. 데이터 모델

- `SoundDatabase`에서 SkillSfx ID 리스트를 제거한다.
- `VfxSoundData`, `VfxSoundCue`를 추가한다.
- 플레이어/몬스터 VFX 사운드 리스트를 별도 직렬화 필드로 노출한다.
- VFX 프리팹 기준 조회 딕셔너리를 초기화한다.

## 2. 런타임 재생

- `AudioManager`는 BGM/SFX ID만 등록한다.
- VFX 사운드 큐 재생 API를 추가해 루프 사운드는 라우팅 AudioSource로, 단발 사운드는 기존 SFX 소스로 재생한다.
- `BattleVfxAudioUtility`는 VFX 프리팹을 `SoundDatabase`에 조회해 큐를 재생하고, 임베디드 `AudioSource`는 제거한다.

## 3. VFX/애니메이터 구조 정리

- `BattleVfxEntry`에서 SFX 필드를 제거한다.
- `BattleProjectileVfxEntry`에서 미사일/임팩트 SFX 필드를 제거한다.
- `BattleUnitAnimator`, 그리드/이벤트/디버그 VFX 호출부는 원본 VFX 프리팹을 공통 유틸에 전달한다.

## 4. 에디터 도구

- `SoundIdDrawer`는 `Bgm`, `Sfx`만 다룬다.
- `SoundUsageScanner`와 브라우저는 ID 사운드와 VFX 직접 매핑을 분리해서 보여준다.
- `SkillVfxAudioAudit`는 VFX 프리팹이 `SoundDatabase`에 직접 연결되어 있는지 검사한다.

## 5. 에셋 이전

- 기존 `skillSfxList` 항목을 플레이어/몬스터 VFX 프리팹 매핑으로 이동한다.
- `SkillVfxDatabase.asset`, 캐릭터/몬스터 프리팹, 관련 씬의 옛 `sfx`, `missileSfx`, `impactSfx` 직렬화 블록을 제거한다.

## 6. 검증

- EditMode 테스트 코드를 새 구조 기준으로 갱신한다.
- MSBuild로 `Assembly-CSharp`와 `Assembly-CSharp-Editor` 컴파일을 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙에 따라 실행하지 않는다.
