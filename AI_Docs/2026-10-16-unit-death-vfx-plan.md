# 유닛 사망 VFX 구현 계획

1. `BattleUnitAnimator`의 사망 VFX 1회 재생 계약을 EditMode 테스트로 먼저 작성한다.
2. 테스트가 현재 구현에서 실패하는 것을 확인한다.
3. `BattleUnitAnimator`에 직렬화된 `deathVfx`와 사망 연출 중복 방지 상태를 추가한다.
4. `PlayDead()`가 최초 호출에서만 애니메이션과 VFX를 재생하도록 구현한다.
5. 대상 테스트, 관련 EditMode 테스트 컴파일, 런타임 C# 컴파일을 검증한다.

