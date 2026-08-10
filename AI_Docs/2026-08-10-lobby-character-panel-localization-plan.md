# 로비 캐릭터 패널 번역 누락 수정 계획

1. 로비 캐릭터 패널 번역 누락 회귀 테스트를 `Assets/Tests/EditMode~/` 아래에 추가한다.
2. 테스트를 실행해 현재 누락 상태가 실패로 잡히는지 확인한다.
3. `Assets/ExcelSource/Localization.xlsx`의 `Text` 시트에 새 키와 5개 언어 번역을 추가한다.
4. `Assets/Language/Text Shared Data.asset`과 각 언어별 `Text_*.asset`에 동일 키를 추가한다.
5. `Assets/Project/Scenes/YDM/Lobby.unity`에서 `Infotext_2`, `Infotext_3`에 `LocalizeStringEvent`를 연결한다.
6. `RuneSettingPanel.ShowRuneSlotInfo`의 잠긴 슬롯 안내 문구를 `GameLocalization` 기반으로 변경한다.
7. `CharacterStatTooltipTarget`의 스탯 툴팁 기본 문구를 `GameLocalization` 기반으로 변경한다.
8. 정적 검증, EditMode 테스트, MSBuild로 결과를 확인한다.
