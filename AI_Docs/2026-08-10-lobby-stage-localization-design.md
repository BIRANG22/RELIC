# 로비 스테이지 선택 문구 로컬라이즈 설계

## 조사 결과

- `ErosionSelectPanel/StageSelectPanel/stage/st1~3` 텍스트는 `Lobby.unity`에 TMP_Text로만 존재하고 `LocalizeStringEvent`가 연결되어 있지 않다.
- `Localization.xlsx`의 `Text` 시트에는 `제 3구역 : 붉은 폐허`, `제 7구역 : 잿빛 수로`, `제 12구역 : 침식 동굴`에 해당하는 키가 없다.
- `LobbyStageButtonCarousel`은 `stageDisplayNames` 값을 `stageTexts`에 직접 쓰는 구조라, 정적 로컬라이저를 붙인 뒤에도 해당 옵션이 켜져 있으면 텍스트가 다시 덮어써질 수 있다.
- 이번 사용자 승인 범위에서 유물 번역값은 수정하지 않는다.

## 권장 설계

- `Localization.xlsx`와 Unity `Text` StringTable에 스테이지 선택 문구용 키 3개를 추가한다.
- 로비 씬의 `st1~3` TMP_Text에 `LocalizeStringEvent`를 연결한다.
- `StageSelectPanel`의 `LobbyStageButtonCarousel.applyStageDisplayNamesToTexts`를 비활성화해서 런타임 하드코딩 문구가 로컬라이즈 결과를 덮어쓰지 않게 한다.

## 키

- `lobby.stage_select.area_3`
- `lobby.stage_select.area_7`
- `lobby.stage_select.area_12`

