# 몬스터 프레젠테이션 인덱스 수정 구현 계획

## 목표

Move를 제외한 몬스터 행동 프레젠테이션을 하나의 연속 슬롯에 안정적으로 매핑한다.

## 작업

1. `Assets/Tests/EditMode~/MonsterPresentationActionIndexTests.cs`에 공격과 비공격 행동이 섞인 회귀 사례를 작성한다.
2. 테스트가 현재 공격/비공격 분리 계산 때문에 실패하는지 확인한다.
3. `MonsterRuntimeData.GetPresentationActionIndexForSkill`의 카운터를 행동 종류와 무관한 단일 카운터로 수정한다.
4. 테스트와 C# 프로젝트 빌드로 검증한다. Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.

