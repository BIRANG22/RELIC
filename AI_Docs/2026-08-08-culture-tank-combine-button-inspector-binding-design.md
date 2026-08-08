# 배양조 조합 버튼 인스펙터 연결 설계

## 원인

`LobbyCultureTankPanelPresenter`가 조합 버튼을 `Content/GameObject/Button` 이름으로 탐색하지만 실제 씬 경로는 `Content/Mix/MixButton`이다. 따라서 `combineButton`이 비어 있고 클릭 이벤트 및 활성화 상태가 적용되지 않는다.

## 설계

- `Lobby.unity`의 Presenter `combineButton` 필드에 `MixButton`의 `Button` 컴포넌트를 직접 연결한다.
- 조합 버튼에 대한 런타임 이름 탐색을 제거한다. 오브젝트 이름이나 계층이 변경되어도 직렬화 참조가 유지된다.
- 인스펙터 참조가 누락된 경우 명확한 오류 로그를 출력한다.
- 씬 테스트에서 직렬화 참조가 실제 `MixButton`을 가리키며, 이름을 변경한 뒤 재바인딩해도 같은 버튼 참조가 유지되는지 검증한다.

## 범위

조합 버튼 연결과 회귀 테스트만 변경한다. 조합 규칙 및 배양조 데이터 처리 방식은 변경하지 않는다.
