# 전역 스킬 보상 ID 제한 구현 계획

1. `SkillRewardPoolPolicy` EditMode 테스트를 먼저 추가한다.
   - 제한 해제 시 원본 후보 유지
   - 제한 사용 시 허용 ID만 유지
   - 공백·중복 ID 정규화
   - 빈 허용 목록이면 후보 없음
2. `SkillRewardPoolDatabase`와 공용 필터 정책을 구현한다.
3. `DataManager`에 설정 에셋 참조를 추가하고 Bootstrap/DebugBattle에 연결한다.
4. 전투, 이벤트, 시작방, 휴식방 상점 스킬 보상 후보 수집 경로에 공용 정책을 적용한다.
5. 정적 검색, C# 프로젝트 빌드, 가능한 EditMode 테스트로 검증한다.
6. 구현 완료 후 별도 코드 리뷰를 수행하고 지적 사항을 반영한다.
