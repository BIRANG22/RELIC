# SampleScene 화살표 Soft Glow 구현 계획

1. 기존 Outline 방식이 없어지고 독립적인 Glow 레이어가 생기는 계약 테스트를 먼저 작성한다.
2. Screen Space Overlay에서 동작하는 UI 전용 Soft Glow 셰이더와 공유 머티리얼을 추가한다.
3. `Arrow` 아래에 `Glow_Left`, `Glow_Right`를 코어보다 앞선 형제 순서로 배치한다.
4. 기존 `Left`, `Right`의 Outline 컴포넌트를 제거한다.
5. 씬 YAML 참조, 테스트 어셈블리와 런타임 어셈블리 컴파일을 검증한다.
6. 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다.
