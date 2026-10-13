# 몬스터 타임라인 점유칸 구현 계획

1. 점유 수용량 정책과 몬스터 명령 점유값에 대한 EditMode 회귀 테스트를 먼저 작성한다.
2. `MonsterSkillData` 및 `MonsterReservedCommand`에 점유칸을 연결한다.
3. 타임라인 미리보기와 컨트롤러 수용량 계산을 실제 점유칸 합계로 변경한다.
4. 일반 예약 및 네트워크 스냅샷 복원에서 요청 행동 전체 점유칸을 검사한다.
5. `GameData.xlsx`와 `GameDataRuntime.csv`의 모든 몬스터 스킬에 `TimelineEntryCost = 1`을 추가한다.
6. 테스트 컴파일, 프로젝트 컴파일, 데이터 일치 여부를 검증한다.
