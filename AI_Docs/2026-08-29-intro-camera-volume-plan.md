# Intro Camera Volume Implementation Plan

## Goal

인트로 Canvas만 카메라 렌더 경로로 전환해 Volume과 포스트프로세싱 적용이 가능하게 한다.

## Steps

1. `IntroSequenceController` 테스트를 추가한다.
2. `IntroSequenceController`에 인트로 Canvas 카메라 모드 설정 메서드를 추가한다.
3. `EnsureIntroCanvasSorting`에서 기존 sorting 보정과 함께 카메라 모드 설정을 적용한다.
4. Bootstrap 씬의 Intro Canvas render mode, camera 참조, Main Camera post processing, Global Volume을 설정한다.
5. 런타임/에디터 어셈블리 컴파일로 검증한다.

## Files

- Modify: `Assets/Project/Scripts/IntroSequenceController.cs`
- Modify: `Assets/Project/Scenes/YDM/Bootstrap.unity`
- Test: `Assets/Tests/EditMode~/IntroSequenceControllerCanvasTests.cs`

## Notes

- 씬 전환 중 카메라 참조가 비어 있으면 `Camera.main`으로 다시 잡는다.
- 인트로 루트가 `DontDestroyOnLoad` 되므로, 카메라가 바뀌는 상황에서도 `EnsureIntroCanvasSorting` 호출 시 재바인딩되게 한다.
