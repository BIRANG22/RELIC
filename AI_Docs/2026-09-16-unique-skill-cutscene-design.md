# Unique Skill Cutscene Design

## 범위

플레이어가 `Category.Unique` 스킬을 실제 실행할 때, 전투 UI 전용 `Skill_cutscene` 프리팹을 표시한다.

## 데이터와 표시 흐름

`CharacterIconDatabase`의 각 `CharacterIconEntry`에 `SkillCutsceneImage`를 연결한다. 전투 실행기는 실제 실행되는 `PlayerReservedCommand`를 프레젠테이션 콜백으로 전달한다. 프레젠터는 유니크 카테고리인지 확인한 뒤 `CharacterId`로 이미지를 조회하고 프리팹의 `mask/image`에 설정한다.

이미지가 없거나 프리팹/표시 루트가 비어 있으면 안전하게 연출을 건너뛴다. 프리팹의 기존 `MaskChainEnableAnimator`가 종료 시 비활성화되며, 프레젠터는 다음 요청에서 같은 인스턴스를 재활성화한다.

## 멀티플레이 경계

이 기능은 확정된 예약 커맨드의 UI 재생만 담당한다. 전투 상태, 비용, 랜덤, 결과 계산에는 관여하지 않는다.
