# Battle Timeline Locked Slot Overlay Build Design

## 원인

`BattleTimelineLockedSlotOverlay`는 `AssetDatabase.LoadAssetAtPath`를
`UNITY_EDITOR` 전처리 구문 안에서만 사용해 거미줄 Sprite를 로드한다.
따라서 Player Build에서는 Sprite가 항상 `null`이며, PNG도 직렬화 참조가 없어
빌드에 포함되지 않는다.

## 수정

- `BattleTimelineBarUI`에 잠긴 슬롯 overlay Sprite의 직렬화 필드를 추가한다.
- 동적으로 생성하는 `BattleTimelineLockedSlotOverlay`에 해당 Sprite를 전달한다.
- `BattleTimelineLockedSlotOverlay`의 Editor 전용 경로 로드를 제거한다.
- Build Settings에 포함된 `YDM/Battle` 씬의 타임라인 바에 `CobwebUI.png`를 직렬화 참조로 연결한다.

## 보존 사항

- 슬롯 잠금 판정과 Elise 패턴 로직은 바꾸지 않는다.
- overlay의 생성, 위치, 투명도와 on/off 동작은 유지한다.
- Resources 경로 및 Editor 전용 AssetDatabase 의존성을 새로 추가하지 않는다.
