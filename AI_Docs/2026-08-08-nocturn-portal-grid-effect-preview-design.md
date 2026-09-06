# 녹턴 포털 그리드 효과 미리보기 설계

## 목표

녹턴 포털 이동 목적지 표시를 별도 스프라이트 생성 방식에서 `GridEffectSpriteDatabase`에 등록된 그리드 효과 프리팹 표시 방식으로 통일한다.

## 설계

- 전용 ID는 `GR_nocturn_portal_preview`를 사용한다.
- `GridEffectSpriteDatabase.asset`에서 이 ID를 `Vfx_Gr_Mon_E_01_move` 프리팹과 연결한다.
- `PlayerSkillReservationController`는 해당 ID로 프리팹을 조회하고 목적지 그리드에 인스턴스를 생성한다.
- 미리보기는 `BattleGridEffectState`에 배치하지 않는다. 이동 판정, 효과 적용, 소모 및 지속시간 계산에 참여하지 않는 연출 전용 객체다.
- 같은 몬스터와 목적지 조합은 기존 참조 카운트를 유지하고, 예약 취소·실행·전체 초기화 시 인스턴스를 제거한다.
- 엑셀과 런타임 CSV에 효과 없는 그리드 효과 행을 함께 추가한다.

## 데이터

`GR_nocturn_portal_preview, 녹턴 포털 예고, Passed=1, Consumable=0, EffectIds=-1, ValueRate=0, CountRate=1, Duration=0, SpawnType=Monster`

## 멀티플레이 경계

기존에 동기화되는 `MonsterReservedCommand`의 `RuntimeId`, `RangeOriginGridIndex`, `IsPortalMove`만 사용한다. 새 네트워크 상태는 추가하지 않으며 각 클라이언트가 동일한 예약 스냅샷으로 미리보기만 재생한다.
