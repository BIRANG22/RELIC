# Monster Death Dissolve Design

## 목표

몬스터가 사망 애니메이션을 끝낸 뒤 즉시 사라지는 대신, SpriteRenderer 전체가 짧은 디졸브 연출을 거쳐 제거되게 한다.

## 구조

- 전투 결과와 사망 판정, 보상, 런타임 몬스터 등록 해제는 `BattleDeathService`의 기존 시점과 흐름을 유지한다.
- `MonsterDeathDissolve`는 전용 Sprite URP 디졸브 머티리얼을 런타임 복제하여 자식 `SpriteRenderer`에 적용하고, MaterialPropertyBlock으로 진행도를 제어하는 순수 연출 컴포넌트다.
- `BattleDeathService`는 사망 애니메이션을 재생한 뒤 디졸브를 시작하고, 두 연출 시간의 합계가 지난 후 게임 오브젝트를 제거한다.
- 공통 머티리얼·셰이더는 모든 몬스터 프리팹에서 공유하며, 시간·경계 색상·경계 폭·노이즈 크기는 프리팹 인스펙터에서 조정한다.

## 흐름

`HP 0 결과 -> 기존 사망 처리/보상/등록 해제 -> Dead 애니메이션 -> MonsterDeathDissolve -> Destroy`

디졸브는 전투 상태를 변경하지 않으며 사망 완료·보상·AI·UI의 판정 근거가 아니다.

## 검증

- EditMode: 디졸브 컴포넌트가 없는 기존 몬스터는 기존 사망 대기 시간으로 유지한다.
- EditMode: 디졸브 컴포넌트가 있는 몬스터는 사망 애니메이션 시간과 디졸브 시간이 합산된 뒤 제거 대기한다.
- Unity Editor: 일반/엘리트/보스 몬스터 각 1종의 사망 애니메이션 뒤 디졸브를 확인한다.
