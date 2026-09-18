# WebGL 브라우저 주도 뷰포트 설계

## 문제

데스크톱용 `ResolutionManager`가 WebGL에서도 `Screen.SetResolution`과 카메라·Root Canvas 보정을 실행한다. 브라우저 Canvas의 CSS 크기는 유지되므로 렌더 영역과 입력 영역이 분리된다.

## 결정

WebGL Player에서는 브라우저 Canvas를 화면 크기의 단일 기준으로 사용한다. `ResolutionManager`는 창 해상도 변경이나 브라우저 Canvas 자체의 크기 변경을 실행하지 않으며, `Screen.width`/`Screen.height`를 바탕으로 카메라 viewport와 검은 레터박스를 갱신한다.

### UI viewport 보정

화면 공간 UI는 카메라의 16:9 viewport와 동일한 콘텐츠 영역을 사용해야 한다. 따라서 WebGL에서도 Root Canvas의 콘텐츠는 `ResolutionCanvasViewportFitter`로 레터박스 내부에 맞춘다. 이 보정은 브라우저 Canvas 자체의 크기·CSS·입력 영역을 변경하지 않는다.

데스크톱 및 에디터의 해상도 선택, 전체화면, Canvas 고정 해상도 보정은 현재 동작을 유지한다.

## 검증

EditMode 테스트에서 WebGL Player가 브라우저 주도 뷰포트 경로를 선택하고, Windows Player는 기존 고정 해상도 경로를 유지함을 검증한다. 실제 WebGL 빌드에서는 전체 Canvas에 클릭과 렌더 영역이 일치하는지 확인한다.
