# Monster Death Dissolve Implementation Plan

1. 공용 `MonsterDeathDissolve.mat`을 셰이더 에셋과 함께 추가한다.
2. `MonsterDeathDissolve`를 독립 컴포넌트로 분리하고, 인스펙터의 시간·경계 색상·경계 폭·노이즈 크기를 런타임 복제 머티리얼에 적용한다.
3. `BattleMonsterSpawner`의 런타임 `AddComponent` 호환 경로를 제거한다.
4. 일반 8종, 엘리트 3종, 보스 1종의 전투 몬스터 프리팹 루트에 공용 머티리얼을 참조하는 컴포넌트를 추가한다.
5. EditMode 회귀 테스트, 컴파일, 프리팹 직렬화 검사를 수행한다.
