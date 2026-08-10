# 로비 스테이지 선택 문구 로컬라이즈 구현 계획

1. `Assets/Tests/EditMode~/LobbyStageLocalizationTests.cs`를 추가해 스테이지 키와 씬 연결 상태를 검증한다.
2. `Assets/ExcelSource/Localization.xlsx`의 `Text` 시트에 스테이지 문구 3개를 추가한다.
3. `Assets/Language/Text Shared Data.asset` 및 `Text_*.asset`에 같은 키와 각 언어 값을 추가한다.
4. `Assets/Project/Scenes/YDM/Lobby.unity`에서 `st1~3`에 `LocalizeStringEvent`를 추가하고, `applyStageDisplayNamesToTexts`를 끈다.
5. batchmode Unity 테스트는 실행하지 않고, 가능한 정적 검증과 컴파일 검증을 수행한다.

