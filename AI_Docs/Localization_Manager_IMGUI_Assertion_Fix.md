# Localization Manager IMGUI Assertion 수정 설계

## 원인

`LocalizationManagerWindow`가 전체 검사 결과의 한글 원문을 IMGUI로 한 번에 렌더링한다. Unity 6의 에디터 기본 동적 폰트가 누락 글리프용 아틀라스를 확장하면서 `DontSaveInEditor` 임시 폰트에 텍스처를 서브 에셋으로 추가하려 해 Assertion이 반복된다.

## 수정

- 프로젝트에 포함된 한글 Font를 Manager 전용 `GUIStyle`에 사용한다.
- 검사 결과는 페이지 단위로 제한해 현재 페이지 항목만 레이아웃·렌더링한다.
- 버튼에서 호출한 검사·에셋 변경은 현재 IMGUI 이벤트가 끝난 다음 `EditorApplication.delayCall`에서 실행한다.
- 중복 클릭을 막고 예약/실행 상태를 창에 표시한다.

## 검증

- 페이지 범위 계산 EditMode 테스트
- Editor 어셈블리 컴파일
- Unity 에디터에서 전체 검사 및 적용 후 신규 Assertion 로그가 발생하지 않는지 확인

