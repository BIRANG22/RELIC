# Console 경고 및 빌드 문제 정리 설계

## Audio

`PlayerSkillReservationController`의 직렬화 기본값과 Battle 씬의 값이 구 ID `SkillReserve`를 사용한다. `AudioIds.Sfx.SkillReserve` 및 SoundDatabase의 정식 ID인 `battle.skill.reserve`로 통일한다.

## Discord Presence

Discord SDK가 반환하는 `NetworkError`, `ClientNotReady`, `Disabled`, `RPCError`는 데스크톱 클라이언트 부재 또는 IPC 연결 불가로 분류한다. 이 경우 상태는 `Unavailable`로 유지하고 로그를 남기지 않는다. 설정/권한/HTTP/초기화 예외는 기존 Warning 또는 Error를 유지한다. 5초 재시도 간격은 변경하지 않는다.

## Partner SDK

임베디드 패키지의 PluginSelector가 PluginImporter 설정을 변경하고 meta를 재임포트한다. 이 동작 중 발생하는 meta 쓰기 오류는 패키지 파일 접근 문제이므로 게임 코드로 우회하지 않는다.

## Shader

서드파티 생성 셰이더 두 개에서 `pow` base인 텍스처 채널만 `max(value, 0)`으로 제한한다. Progress/시간 등 효과 제어 입력은 변경하지 않는다.
