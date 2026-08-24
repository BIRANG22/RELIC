# Boss Result Panel Canvas, Stats, And Experience Plan

1. 결과 빌더 테스트를 추가해 스냅샷 기반 0 통계 row와 기존 통계 병합을 검증한다.
2. 스테이지 클리어 경험치 정산 서비스 테스트를 추가한다.
3. 결과 패널 소스/씬 테스트를 갱신해 씬 배치, 루트 Canvas 부모, side image 우선 사용을 검증한다.
4. `ExplorationResultData.cs`에 통계 병합과 경험치 정산 서비스를 구현한다.
5. `ExplorationResultPanelUI`와 row UI를 수정해 경험치 표시, side image, 기본 0 표시를 연결한다.
6. `Battle.unity`에서 `ExplorationResultPanel`을 루트 Canvas 자식으로 이동한다.
7. MSBuild와 소스 검증으로 컴파일 및 씬 YAML 중복 ID를 확인한다.
