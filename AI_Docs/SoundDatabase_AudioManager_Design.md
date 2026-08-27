# SoundDatabase 기반 오디오 관리 설계

## 조사 결과

- 현재 `AudioManager`는 Bootstrap 씬의 인스펙터 목록(`bgmList`, `commonSfxList`, `uiSfxList`, `battleSfxList`, `vfxSfxIdList`)에서 BGM/SFX 딕셔너리를 만든다.
- 새 enum 기반 사운드는 `SfxType` 또는 `BgmType`에 항목을 추가하고, 호출부에서 `PlaySfx(SfxType)` 또는 `PlayBgm(BgmType)`를 호출한다.
- 전투 스킬 VFX 사운드는 `SkillVfxDatabase`의 `BattleVfxEntry.sfx.sfxId`를 사용할 수 있지만, 현재 등록된 스킬 항목은 `playSfx`가 꺼져 있고 `sfxId`가 비어 있다.
- 현재 실제 VFX 사운드는 VFX 프리팹 내부 `AudioSource`를 `BattleVfxAudioUtility`가 찾아 `AudioManager.PlaySfxClip(AudioSource)`로 우회 재생하는 구조다.

## 권장 설계

- `SoundDatabase` ScriptableObject를 추가해 사운드 ID와 AudioClip 매칭을 한 곳에서 관리한다.
- `AudioManager`는 `SoundDatabase`를 참조해 BGM/SFX ID 딕셔너리를 초기화한다.
- 기존 Bootstrap 씬의 직렬화된 사운드 목록은 `SoundDatabase.asset`으로 이관하고, `AudioManager` 내부의 직접 사운드 리스트는 제거한다.
- 기존 enum 호출부는 당장 대규모 씬/프리팹 마이그레이션을 하지 않도록 enum 이름을 DB alias로 조회하는 호환 레이어로만 유지한다.
- 새 사운드는 `SoundDatabase`에 ID/클립을 등록하고 `PlaySfx(string id)` 또는 VFX/프레젠테이션 데이터의 `sfxId`로 연결한다.
- string 사운드 ID 필드는 `[SoundId(SoundCategory.Sfx)]` 또는 `[SoundId(SoundCategory.Bgm)]`를 붙이면 인스펙터에서 DB에 등록된 ID를 드롭다운으로 선택할 수 있다.
- 각 사운드 항목은 `volume`, `pitch`, `useRandomPitch`, `randomPitchMin`, `randomPitchMax`를 가진다. 랜덤 pitch는 연출용 변주이며 전투 결과에는 관여하지 않는다.

## 멀티플레이 경계

- 전투 핵심 로직은 사운드를 직접 재생하지 않는다.
- 전투 결과에 따른 연출 계층이 사운드 ID 또는 VFX 내장 AudioSource를 재생한다.
- 이번 변경은 사운드 조회/관리 경로만 바꾸며 전투 결과 계산, 랜덤, 네트워크 동기화 모델은 변경하지 않는다.

## 구현 계획

1. `SoundDatabase`와 BGM/SFX cue 데이터 타입을 추가한다.
2. `AudioManager`가 DB에서 딕셔너리를 구성하도록 변경한다.
3. 기존 Bootstrap 씬 목록을 `Assets/DB/SoundDatabase.asset`으로 이관하고 `AudioManager`에 연결한다.
4. `SoundIdAttribute`와 Editor 전용 `SoundIdDrawer`를 추가해 string ID 필드에서 DB 기반 드롭다운 선택을 지원한다.
5. DB 사운드 항목에 pitch와 랜덤 pitch 옵션을 추가하고 `AudioManager` 재생 경로에서 적용한다.
6. 기존 enum 기반 호출과 string ID 호출이 DB를 통해 동작하는지 EditMode 테스트를 추가한다.
7. C# 빌드로 컴파일 검증을 수행한다.
