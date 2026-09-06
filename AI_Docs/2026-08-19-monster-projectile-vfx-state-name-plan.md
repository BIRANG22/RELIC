# Monster Projectile VFX StateName Plan

## 조사 결과

- 몬스터 일반 액션 VFX는 `monsterActionPresentations`의 액션 슬롯과 `stateName` 기준으로 재생한다.
- 몬스터 projectile VFX에 별도 `skillId` fallback 매칭이 추가되어 있어 액션 슬롯과 다른 VFX가 선택될 수 있었다.
- Arabella `AttackAction2`에는 `impactPrefab`만 연결되어 있고 `missilePrefab`은 비어 있었다.
- 기존 `HasProjectileVfx`는 `missilePrefab`만 유효 조건으로 보아 impact-only 설정이 재생 경로에 들어가지 못했다.

## 권장 설계

- 몬스터 projectile VFX 선택은 `MonsterReservedCommand.ActionIndex`로 매핑되는 `monsterActionPresentations` 슬롯만 사용한다.
- `BattleProjectileVfxEntry.skillId`는 런타임 매칭과 인스펙터 설정 모두에서 제거한다.
- projectile VFX는 `missilePrefab` 또는 `impactPrefab` 중 하나라도 있으면 유효한 설정으로 본다.
- `missilePrefab` 없이 `impactPrefab`만 있으면 이동 연출 없이 타겟 위치에 impact VFX만 생성한다.

## 영향 범위

- 전투 결과 계산은 변경하지 않고 VFX 선택 및 재생 조건만 변경한다.
- 멀티플레이 동기화 대상 상태에는 영향이 없다.
