# 로비 준비 패널 BackButton 입력 수정 설계

## 원인

`Ready_Panel`은 `overrideSorting`이 활성화된 별도 Canvas와 GraphicRaycaster를 사용하지만, `BackButton`은 그 바깥의 부모 Canvas에 배치되어 있었다. 이 때문에 형제 순서 변경만으로는 Canvas 정렬 경계를 넘을 수 없고, 준비 패널이 버튼보다 앞에서 입력을 받았다.

## 설계

- `BackButton`을 `Ready_Panel` RectTransform의 마지막 자식으로 이동한다.
- 재부모화 전후의 화면 위치가 같도록 로컬 anchored position을 보정한다.
- 런타임 버튼 탐색은 `Ready_Panel/BackButton`을 우선하고, 기존 계층은 호환용 fallback으로 유지한다.
- 씬 YAML 계층을 검사하는 EditMode 회귀 테스트를 추가한다.

## 영향 범위

- 로비 준비 패널의 UI 정렬과 BackButton 입력 처리만 변경한다.
- 전투 상태, 랜덤, 네트워크 동기화 구조에는 영향을 주지 않는다.
