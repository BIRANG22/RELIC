# Audio Enum To Sound ID Migration

## Goal

오디오 재생 코드를 `BgmType`/`SfxType` enum 중심에서 `SoundDatabase` ID 중심으로 전환한다.

## Design

- 코드 자동완성용 상수는 `AudioIds.Bgm.*`, `AudioIds.Sfx.*`로 제공한다.
- 인스펙터에서 고르는 사운드 필드는 `[SoundId(SoundCategory.Bgm)] string`, `[SoundId(SoundCategory.Sfx)] string`으로 선언한다.
- `AudioManager`는 문자열 ID만 직접 받는 API를 유지한다.
- 배틀 주사위와 보스방 연출처럼 `AudioClip`을 직접 재생하던 흐름은 DB ID를 통해 `AudioManager`로 재생한다.
- 기존 씬/프리팹에 저장된 enum 숫자값은 같은 의미의 ID 문자열로 마이그레이션한다.

## Verification

- `BgmType`/`SfxType` 참조가 남지 않는지 검색한다.
- `SoundDatabase`에 실제 사용 ID가 등록되어 있는지 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않고, C# 프로젝트 빌드로 컴파일을 확인한다.
