# Intro Camera Volume Design

## Goal

Bootstrap 인트로 UI가 카메라 포스트프로세싱과 Volume 영향을 받을 수 있도록 인트로 Canvas만 `Screen Space - Camera` 렌더 경로로 이동한다.

## Current State

- Bootstrap 씬의 `Intro` 오브젝트는 Canvas이며 `Screen Space - Overlay`로 렌더링된다.
- `IntroSequenceController`는 런타임에 `introRoot`의 Canvas sorting 값을 보정한다.
- Bootstrap의 Main Camera와 VFX Camera는 URP `Render Post Processing`이 꺼져 있다.
- Bootstrap/Title 씬에는 인트로 전용 `Global Volume`이 없다.

## Design

- `IntroSequenceController`에 인트로 Canvas 렌더 모드 설정을 추가한다.
- 기본값은 인트로 Canvas를 `Screen Space - Camera`로 만들고, 전용 Camera가 비어 있으면 `Camera.main`을 사용한다.
- 인트로 카메라의 URP `renderPostProcessing`을 켤 수 있게 한다.
- Bootstrap 씬의 `Intro` Canvas는 `Screen Space - Camera`로 저장하고 Main Camera를 연결한다.
- Bootstrap 씬에 인트로용 `Global Volume`을 추가해 카메라가 Volume 값을 읽을 수 있게 한다.

## Constraints

- 전투 핵심 로직은 변경하지 않는다.
- 일반 로비/전투 UI Canvas 설정은 변경하지 않는다.
- 인트로 UI 오브젝트 구조와 기존 `IntroSequenceController` line action 흐름은 유지한다.

## Verification

- `IntroSequenceController.ConfigureIntroCanvasForTest` 테스트로 Canvas render mode, camera, plane distance, post processing 설정을 확인한다.
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 컴파일을 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않는다.

## Multiplayer Impact

에디터/인트로 프레젠테이션 설정 변경만 포함하며 전투 결과 동기화나 런타임 전투 상태에는 영향이 없다.
