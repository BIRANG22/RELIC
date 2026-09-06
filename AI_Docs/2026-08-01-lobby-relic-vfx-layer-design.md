# 로비 유물 VFX 정렬 설계

## 목표

- `RelicShopPanel`의 세 유물 슬롯에서 `magic_ring_06` VFX가 유물 아이콘을 가리지 않게 한다.
- VFX는 카드 배경 앞에 유지하고 유물 아이콘 뒤에만 표시한다.

## 원인

- `magic_ring_06`의 활성 파티클 렌더러는 `Unit` Sorting Layer에서 최대 Order 5를 사용한다.
- `RelicIcon`은 일반 Canvas 계층의 `Image`이므로 파티클 렌더러가 UI 형제 순서보다 앞에 그려질 수 있다.

## 설계

- `RelicOffer_1~3/RelicIcon` 각각에 중첩 `Canvas`를 추가한다.
- 중첩 Canvas는 `overrideSorting = true`, Sorting Layer `Unit`, Order 10을 사용한다.
- VFX 프리팹과 파티클 설정은 변경하지 않는다.
- 아이콘의 `Image`, 클릭, 호버 확대 동작은 기존 구조를 유지한다.

## 멀티플레이 경계

- 씬 UI 렌더 순서만 변경하며 런타임 상태와 동기화 데이터에는 영향을 주지 않는다.

