# Monster Death Dissolve Implementation Plan

1. SpriteRenderer용 URP 디졸브 Shader Graph와 공용 머티리얼을 추가한다.
2. `MonsterDeathDissolve` 컴포넌트로 렌더러별 런타임 머티리얼과 디졸브 진행도를 관리한다.
3. `BattleDeathService`가 사망 애니메이션 후 디졸브를 재생하고 완료 시 제거하도록 대기 시간을 연결한다.
4. 모든 전투 몬스터 프리팹에 공용 머티리얼을 지정한 컴포넌트를 추가한다.
5. EditMode 회귀 테스트와 Unity Editor 수동 검증으로 확인한다.
