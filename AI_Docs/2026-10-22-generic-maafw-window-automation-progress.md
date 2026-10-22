# 범용 MaaFramework 창 자동화 구현 결과

## 구현 결정

- 실행 시 열려 있는 최상위 창을 열거하고 사용자가 HWND 대상을 명시적으로 선택한다.
- 캡처는 MaaFramework `Win32ScreencapMethods.Background`, 입력은 `SendMessage`를 사용한다.
- 프로필은 앱 코드와 분리된 `profile.json + resource/pipeline/*.json + resource/image/*.png` 구조다.
- 한 회 성공은 Maa 작업 상태 성공과 `targetNode`의 실제 실행을 모두 만족해야 한다.
- 반복 횟수 상한은 10,000회이며 창 식별자·프로세스·제목·크기 변화와 전역 긴급 중지를 안전 정지 조건으로 사용한다.

## TDD 기록

- 프로필 로더 테스트를 먼저 추가하여 미구현 컴파일 실패를 확인한 뒤 구현했다.
- 프로필 기준 해상도 불일치 테스트를 먼저 추가하여 `Completed` 오동작을 확인한 뒤 입력 전 중지 로직을 구현했다.
- 최종 Core 테스트 18개가 통과했다.

## 빌드 결과

- Debug 전체 빌드 성공, 경고 0개, 오류 0개
- Release `win-x64` framework-dependent publish 성공
- 배포 경로: `Tools/RelicAutomation/src/RelicAutomation.App/bin/Release/net8.0-windows/win-x64/publish`

## 남은 실제 연동 확인

실제 RELIC 화면 캡처가 제공되지 않아 샘플의 `start.png`, `target.png`는 포함하지 않았다. 사용자가 두 이미지를
추가한 뒤 실제 창에서 백그라운드 캡처와 `SendMessage` 호환성을 확인해야 한다. 프로그램은 PNG가 없으면 시작 전에
프로필 검증 오류로 중지한다.
