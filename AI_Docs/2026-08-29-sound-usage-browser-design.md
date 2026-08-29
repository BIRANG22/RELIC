# Sound Usage Browser Design

## Goal

사운드 ID, AudioClip, 스킬/VFX 프레젠테이션, 프리팹 내장 AudioSource 사용처를 한 곳에서 역추적하고, SoundDatabase의 파일 교체를 에디터에서 바로 할 수 있게 한다.

## Current Context

- `Assets/DB/SoundDatabase.asset`가 `bgmList`, `sfxList`, `skillSfxList`를 가진다.
- `AudioManager`는 `SoundDatabase`의 BGM/SFX/Skill SFX를 ID lookup으로 등록한다.
- 스킬 VFX 사운드는 `BattleVfxEntry.sfx`, `additionalSfx`, `BattleProjectileVfxEntry.missileSfx`, `impactSfx`의 Skill SFX ID를 참조한다.
- 기존 `SkillVfxAudioAudit`는 스킬 VFX와 캐릭터/몬스터 프리젠테이션의 DB SFX 연결 여부와 embedded AudioSource 잔존 여부를 Markdown으로 출력한다.
- 사운드가 DB, 스킬 VFX DB, 캐릭터 프리팹, 몬스터 프리팹, 일반 `[SoundId]` 필드로 흩어져 전체 사용처를 한 번에 보기 어렵다.

## Target Workflow

1. `Relic/Audio/Open Sound Usage Browser`를 연다.
2. 왼쪽에서 SoundDatabase의 ID를 선택한다.
3. 오른쪽에서 해당 ID가 어디서 참조되는지 확인한다.
4. SoundDatabase 항목의 clip, volume, pitch, loop 값을 바로 수정한다.
5. `Relic/Audio/Generate Sound Usage Report`로 현재 상태를 `AI_Docs/sound-usage-audit.md`에 저장한다.

## Scanner

`SoundUsageScanner`는 에디터 전용 정적 서비스다.

- SoundDatabase 항목을 `SoundUsageDatabaseEntry`로 수집한다.
- 모든 프리팹을 `SerializedObject`로 순회해 `[SoundId]` 속성이 붙은 string 필드를 찾고 참조를 수집한다.
- `SkillVfxDatabase`의 `BattleVfxEntry.sfx`와 캐릭터/몬스터 `BattleUnitAnimator` 프리젠테이션은 명시적 컨텍스트 이름을 붙여 수집한다.
- 모든 프리팹의 `AudioSource`를 스캔해 embedded AudioSource 사용처를 수집한다.
- DB에 없는데 참조되는 ID는 `MissingDatabaseEntry`로 표시한다.
- DB에 있지만 참조가 없는 ID는 `Unused`로 표시한다.

## Editor Window

`SoundUsageBrowserWindow`는 `EditorWindow` 기반이다.

- 상단 버튼: Refresh, Generate Report, Ping Database.
- 왼쪽 목록: category, id, clip, usage count, status.
- 오른쪽 상세:
  - SoundData의 clip/volume/pitch/loop 편집 UI.
  - 해당 ID 참조 목록.
  - embedded AudioSource 목록.
  - missing/unused 상태 표시.
- 편집은 `SerializedObject`를 통해 `SoundDatabase.asset`에 기록하고 `AssetDatabase.SaveAssets()`로 저장한다.

## Report

Markdown 리포트는 `AI_Docs/sound-usage-audit.md`에 생성한다.

섹션:

- Summary
- Database Entries
- Usage By Sound ID
- Missing Database Entries
- Unused Database Entries
- Embedded AudioSources

## Testing

- `SoundUsageScanner`가 SoundDatabase 항목과 `[SoundId]` 참조를 수집하는지 테스트한다.
- 스킬 VFX의 main/additional SFX ID를 수집하는지 테스트한다.
- DB에 없는 참조와 미사용 DB 항목을 분류하는지 테스트한다.
- embedded AudioSource의 clip/loop/volume/pitch 정보를 수집하는지 테스트한다.
- Markdown 리포트에 핵심 섹션과 참조 위치가 포함되는지 테스트한다.

## Multiplayer Boundary

이 기능은 에디터 전용 사운드 관리/검증 도구다. 런타임 전투 결과나 Command -> State Change -> Result/Event 구조를 변경하지 않는다.
