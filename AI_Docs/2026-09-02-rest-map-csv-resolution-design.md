# RestRoom CSV 기반 Map ID 해소 설계

수동 맵의 `Rest` 타입이 CSV 후보를 찾지 못할 때 문자열 `Rest`를 Map ID로 사용하는 폴백을 제거한다.

- 모든 수동 맵 노드는 `MapData.Type`과 `Stage`가 일치하는 CSV 데이터에서 실제 Map ID를 선택한다.
- Rest 데이터가 없으면 가짜 Map ID를 생성하지 않고 수동 맵 생성이 실패한다.
- 맵 생성 키에 `MapDataV2` 버전을 포함해, 이전에 `Rest` ID로 저장된 런타임 맵이 자동 재생성되게 한다.
- 현재 CSV의 `Map_26, Rest, Rest, 0, 0, Stage1, 1`이 Rest 노드의 실제 Map ID가 된다.
