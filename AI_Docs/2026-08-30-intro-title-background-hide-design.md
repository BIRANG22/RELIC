# Intro Title Background Hide Design

## Goal

타이틀에서 시작 버튼으로 인트로를 재생하는 동안 Title 씬의 루트 `Background` 오브젝트가 인트로 화면을 가리지 않도록 비활성화한다.

## Current State

- `StartModeButton`은 첫 시작 시 `IntroSequenceController.PlayFirstTimeIntro()`를 호출한다.
- `IntroSequenceController`는 인트로 표시 중 Overlay Canvas를 숨기고 종료 시 복구하는 기능을 이미 갖고 있다.
- Title 씬의 `Background`는 루트 GameObject이며 Canvas가 아니므로 기존 Canvas 숨김 로직의 대상이 아니다.
- Bootstrap의 `IntroSequenceController`는 DontDestroyOnLoad 오브젝트라 Title 씬 오브젝트를 정적 씬 참조로 직접 연결하기 어렵다.

## Design

- `IntroSequenceController`에 인트로 동안 숨길 일반 GameObject 목록과 자동 탐색 이름 목록을 추가한다.
- 인트로가 표시될 때 대상 GameObject의 기존 `activeSelf` 상태를 저장한 뒤 `SetActive(false)` 한다.
- 인트로가 종료되거나 컨트롤러가 파괴될 때 저장된 상태를 복구한다.
- 기본 자동 탐색 이름은 `Background`로 두어 Title 씬 루트 배경을 별도 씬 연결 없이 숨긴다.
- 기존 `hideWhileIntroVisible` Canvas 숨김 로직은 유지한다.

## Verification

- EditMode 테스트로 GameObject 숨김/복구 유틸 동작을 검증한다.
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 컴파일을 확인한다.
- 프로젝트 규칙에 따라 Unity batchmode 테스트는 실행하지 않는다.

## Multiplayer Impact

타이틀/인트로 프레젠테이션 오브젝트 표시 상태만 변경하며 전투 상태, 랜덤, 동기화 데이터에는 영향이 없다.
