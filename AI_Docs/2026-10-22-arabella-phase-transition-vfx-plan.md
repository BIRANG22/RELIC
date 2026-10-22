# 아라벨라 페이즈 전환 VFX 구현 계획

1. 전환 VFX가 중복 생성되지 않는 EditMode 테스트를 먼저 작성하고 실패를 확인한다.
2. `BattleUnitAnimator`에 페이즈 전환 VFX 슬롯과 1회 재생 방어를 추가한다.
3. `PlayArabellaPhaseTransition()`에서 애니메이션 시작과 동시에 기존 VFX 경로를 호출한다.
4. `Arabella.prefab`에 새 직렬화 슬롯을 반영한다.
5. 관련 EditMode 테스트와 C# 프로젝트 컴파일로 검증한다.
