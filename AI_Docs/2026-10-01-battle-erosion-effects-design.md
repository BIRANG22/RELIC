# 배틀 침식 난이도 효과 적용 설계

## 목표

로비 `ErosionSelectPanel`에서 선택한 `DifficultyId`를 탐사 시작 시 고정하고, 데이터의 `EffectType`/`EffectValue`가 실제 탐사 및 전투 결과에 반영되게 한다.

## 구조

- 로비 선택값은 탐사 시작 시 `BattleRuntimeData.SelectedErosionDifficultyIds`로 복사한다.
- `BattleErosionEffectService`가 배틀 스냅샷과 `ErosionDatabase`를 읽어 효과별 최종 값을 계산한다.
- `Exclusive` 효과는 같은 `GroupId`에서 가장 높은 Tier 하나만 사용하고 `Independent` 효과는 모두 합산한다.
- UI는 결과를 계산하지 않으며 전투/맵/보상 시스템이 서비스의 순수 계산 API를 호출한다.
- 선택 ID와 일회 적용 상태는 저장 및 전투 네트워크 스냅샷에 포함되는 `BattleRuntimeData`에 보관한다.

## 적용 경계

- 탐사 시작: 파티 최대 HP 감소, 마지막 유물 슬롯 잠금
- 몬스터 생성/공격: 최대 HP 및 최종 피해 증가
- 턴 시작: 주기적 무작위 해로운 상태 부여 (`BattleRandom` 사용)
- 맵/지도: 보스 전 휴식방 제거, 다음 방 타입 은닉
- 상점/보상: 가격 증가, 희귀도 단계 하향
- 이벤트/행동: 주사위 난이도 및 이동 최종 비용 증가
- 장비: 연성제 사용 차단

## 검증

- 선택 ID 복사와 그룹 해석
- 퍼센트 반올림 및 최솟값
- 각 EffectType 정책 계산
- 일회 적용 중복 방지
- 프로젝트 C# 컴파일

## 데이터 정합성

`Erosion_03_03`은 표시 문구의 20% 감소와 일치하도록 `EffectValue`를 `-20`으로 교정한다.
