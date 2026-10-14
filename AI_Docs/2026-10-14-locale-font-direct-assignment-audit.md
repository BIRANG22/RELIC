# 언어별 폰트 역할 적용 직접 지정 감사

## 이번 적용 범위

- `Option.prefab`의 모든 TMP에 `LocalizedTMPFont`를 배치했다.
- 일반 옵션 라벨과 Dropdown Template은 `body`, 수치 표시 `Value`는 `number` 역할을 사용한다.
- `OptionTMPFontEnforcer`는 직렬화 호환을 위해 유지하되 폰트를 직접 지정하거나 매 프레임 덮어쓰지 않고, 배치된 `LocalizedTMPFont`의 갱신만 요청한다.
- 전멸 패배 문구에는 `title` 역할을 연결했다.

## 유지한 직접 폰트 지정

- `UIBlurReplicaSource`: 원본 TMP의 최종 폰트 결과를 복제하는 표현 계층이므로 유지한다.
- `TMPKoreanGlyphPreloader`: glyph preload 후 원본 폰트를 복원하는 보조 흐름이므로 유지한다.
- `TimelineSkillHoverPopupView`: 별도 직렬화 폰트를 사용하는 기존 전투 팝업이며 이번 점진적 마이그레이션 범위 밖이다.
- `PlayerSkillReservationController`: 이동 비용 팝업 인스턴스에 기존 직렬화 폰트를 적용하며 이번 범위 밖이다.
- `BattleSkillTextPopupUI`: 전투 팝업 전용 폰트 지정이며 이번 범위 밖이다.

## 후속 적용 원칙

위 UI를 역할 DB로 전환할 때는 직접 `TMP_Text.font` 지정을 먼저 제거하고, 프리팹 또는 씬에 `LocalizedTMPFont`를 배치하여 폰트 소유자가 둘 이상 생기지 않게 한다. 프로젝트 전체 자동 마이그레이션은 수행하지 않는다.
