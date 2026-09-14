# 환경 프리팹 ambience 오디오 설계

## 목적

- BGM 상태에 종속되어 함께 재생되는 기존 `BgmData.ambienceClips`는 유지한다.
- 환경 프리팹이 활성 상태인 동안 독립적인 2D ambience를 반복 재생할 수 있게 한다.

## 데이터

- `SoundDatabase`의 `bgmList` 바로 다음에 `ambienceList`를 추가한다.
- 각 항목은 기존 `SoundData`를 재사용한다. ID는 SFX와 동일한 문자열 형식(예: `ambience.forest.wind`)이며, `loop`은 ambience 항목에서 기본으로 활성화한다.
- `SoundCategory.Ambience`와 `SoundIdDrawer`를 확장해 ambience 전용 ID만 고를 수 있게 한다.

## 재생

- `EnvironmentAmbienceAudioSource`는 프리팹에 붙이는 전용 컴포넌트다.
- 활성화 시 설정된 ambience ID를 `AudioManager`를 통해 2D로 재생하고, 비활성화 또는 파괴 시 자신이 시작한 재생만 정지한다.
- 컴포넌트를 재활성화해도 동일 인스턴스에서 루프가 중복되지 않는다.
- 재생 위치를 전달하지 않고 AudioManager의 SFX 템플릿 설정 중 `spatialBlend`만 0으로 강제한다.
- ambience는 SFX 재생 소스와 분리해 `Master × BGM` 볼륨 경로를 사용한다.

## 범위와 멀티플레이 경계

- 환경음은 프리젠테이션 전용이며 전투 상태, 결과, 랜덤, 네트워크 동기화에 영향을 주지 않는다.
- 신규 상태 enum 또는 switch는 만들지 않으며, 프리팹 인스펙터에서 문자열 ID를 선택한다.

## 검증

- ambience DB 목록이 SFX 목록과 분리되어 조회되는지 확인한다.
- ambience용 ID 드롭다운이 ambience 목록만 제공하는지 확인한다.
- 컴파일 검증을 수행한다. Unity batchmode 테스트는 프로젝트 규칙에 따라 실행하지 않는다.
