# 범용 MaaFramework 창 자동화 실행기 사용법

## 빌드

PowerShell에서 `Tools/RelicAutomation/build.ps1`을 실행한다. 결과물은
`Tools/RelicAutomation/src/RelicAutomation.App/bin/Release/net8.0-windows/win-x64/publish`에 생성된다.

## 프로필 구성

각 프로필은 `Tools/RelicAutomation/Profiles/<프로필 이름>` 아래에 둔다.

- `profile.json`: 진입 노드, 목표 노드, 반복 횟수, 기준 클라이언트 크기
- `resource/pipeline/*.json`: MaaFramework 작업 흐름
- `resource/image/*.png`: TemplateMatch용 무손실 캡처

`RELIC-Sample`은 흐름 예시다. 실행 전 대상 창을 기준 해상도로 맞춘 뒤 다음 두 이미지를 직접 캡처해
`resource/image`에 넣어야 한다.

- `start.png`: 클릭할 시작 버튼의 720p 기준 무손실 원본
- `target.png`: 한 번의 작업이 완료됐다고 판정할 목표 화면

프로필의 `TargetReached` 노드가 실제로 실행된 경우에만 한 회 성공으로 계산한다. 목표 노드가 실행되지
않거나, 대상 창이 사라지거나, 창 제목/프로세스/크기가 바뀌면 반복을 중단한다.

## 실행

1. 실행기에서 `새로고침`을 누른다.
2. 소유하거나 테스트 권한이 있는 대상 창을 고른다.
3. 프로필을 고른다.
4. 선택된 창 제목, 프로세스와 크기를 확인하고 `시작`을 누른다.
5. 언제든 `중지` 또는 전역 단축키 `Ctrl+Shift+F12`로 정지한다.

백그라운드 캡처는 MaaFramework의 `FramePool | PrintWindow` 조합을, 입력은 `SendMessage`를 사용한다.
대상 프로그램의 렌더링 방식이나 권한 수준에 따라 백그라운드 캡처·입력을 지원하지 않을 수 있다.
대상 프로그램이 관리자 권한이라면 실행기도 같은 권한이 필요할 수 있다.
