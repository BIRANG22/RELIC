# 전투 보상 유물 교체 제거 설계

## 의도

전투 보상 화면에서는 이미 장착된 유물을 새 보상으로 덮어쓰지 않는다. 보상 유물은 타입에 맞는 빈 슬롯에만 장착하며, 빈 슬롯이 없으면 기존 추출 흐름으로 처리한다. 로비의 유물 해제와 캐릭터 간 교환은 유지한다.

## 현재 구조

- Battle 씬에는 `BattleRewardRelicPanelUI`만 실제 배치되어 있다.
- `BattleRewardEquipPanelUI`는 씬/프리팹에 배치되지 않았지만 여러 보상 호출의 정적 중계점과 `BattleRewardPanelUI`의 fallback 타입으로 남아 있다.
- `BattleRewardRelicPanelUI`는 점유된 패시브 슬롯을 직접 선택하면 해당 슬롯을 덮어쓸 수 있다.
- `BattleRewardEquipPanelUI`의 자체 유물 로직은 빈 슬롯만 허용하지만 실제 컴포넌트는 사용되지 않는다.

## 선택한 접근

실제 패널을 직접 호출하도록 진입점을 정리하고 미사용 MonoBehaviour를 제거한다.

- 유물 보상 호출은 `BattleRewardRelicPanelUI.TryOpenRelicReward`를 사용한다.
- 기억 보상 호출은 `BattleRewardSkillPanelUI.TryOpenSkillReward`를 사용한다.
- `BattleRewardPanelUI`의 미사용 `equipPanel` fallback과 Blur 수집 참조를 제거한다.
- 참조가 모두 제거되면 `BattleRewardEquipPanelUI` 스크립트를 삭제한다.
- 유물 장착 가능 슬롯 계산은 순수 정책으로 분리하고 실제 유물 패널은 첫 번째 빈 호환 슬롯만 사용한다.

## 동작 규칙

- 액티브 유물은 액티브 전용 슬롯이 비어 있을 때만 장착한다.
- 패시브 유물은 패시브 6칸 중 첫 번째 빈 슬롯에만 장착한다.
- 점유 슬롯 클릭은 장착 대상을 바꾸거나 기존 유물을 덮어쓰지 않는다.
- 호환 빈 슬롯이 없는 캐릭터는 선택해도 장착되지 않는다.
- 추출, 로비 해제/교환, 이벤트 비용으로 유물을 제거하는 흐름은 변경하지 않는다.

## 가시 반사 충돌 복구

현재 `PierceEffect`에는 앞서 구현한 `BattleWardReactionService` 호출만 제거되어 있고, 이후 추가된 `SkillImpactResult` 기록은 남아 있다. 현 파일을 기준으로 피격 전 가시값 캡처와 실제 관통 피해 후 반사 호출만 복구하여 양쪽 변경을 보존한다.

## 멀티플레이 경계

보상 장착 결과는 기존 `CharacterRuntimeData.EquippedRelicIds` 상태 변경을 유지한다. UI는 빈 슬롯 정책의 결과만 요청하며 로비 장비 조작이나 네트워크 프레임워크에는 영향을 주지 않는다.
