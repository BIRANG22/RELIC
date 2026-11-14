# 몬스터 정보 패널 상태효과 아이콘 색상 설계

## 의도

몬스터 정보 패널의 버프·디버프·고유 특성이 효과별 아이콘과 의미에 맞는 색상으로 표시되어 전투 HUD와 시각적으로 일관되게 한다.

## 현재 구조

- `BattleMonsterInfoCanvasUI`가 `MonsterStatus.prefab`을 생성하고 `MonsterInfoStatusItemUI.Bind`를 호출한다.
- `MonsterInfoStatusItemUI`는 `StatusEffectIconDatabase`에서 효과별 스프라이트만 조회하고 Image 색상은 변경하지 않아 프리팹 기본 흰색이 유지된다.
- 전투 HUD의 `StatusEffectIcon`은 같은 데이터베이스의 `GetIconColor`를 사용해 버프·디버프·고유 특성 색상을 이미 적용한다.

## 접근 방식

1. 선택: `MonsterInfoStatusItemUI`가 `StatusEffectIconDatabase.GetIconColor`를 호출한다. 기존 HUD와 색상 설정을 공유하고 변경 범위가 가장 작다.
2. 제외: 정보 패널 전용 색상을 새로 직렬화한다. 동일 의미에 서로 다른 색상이 생기고 조정 지점이 중복된다.
3. 제외: 효과별 색상 필드를 데이터베이스 엔트리에 추가한다. 현재 요구는 버프·디버프 구분이며 데이터 마이그레이션 범위가 불필요하게 커진다.

## 표시 규칙

- 효과별 스프라이트는 기존 `TryGetIcon` 결과를 사용한다.
- 색상은 기존 `GetIconColor(effectId, effectDatabase, isMonsterInnateTrait)` 결과를 사용한다.
- 고유 특성은 `innateTraitColor`, 버프는 `beneficialColor`, 디버프는 `harmfulColor`, 중립·미등록 효과는 흰색을 사용한다.
- 프리팹에 설정된 알파는 유지하면서 RGB만 효과 색상으로 변경한다.
- 데이터가 없거나 항목을 비울 때는 아이콘 색상을 흰색으로 복원한다.

## 범위 밖

- 전투 결과 및 상태효과 판정 변경
- 새로운 아이콘 리소스 제작
- 상태효과 데이터 또는 네트워크 모델 변경
