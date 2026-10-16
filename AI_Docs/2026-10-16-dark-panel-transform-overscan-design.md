# 어두운 전체 화면 패널 Transform 확대 설계

## 원인

패널 뒤에 배치된 어두운 전체 화면 패널의 RectTransform이 화면 경계와 정확히 일치하면 Canvas 스케일링과 픽셀 반올림으로 가장자리 1픽셀이 노출될 수 있다.

## 설계

- 별도 Image나 메시 보정이 아니라 실제 어두운 패널 RectTransform을 확대한다.
- 사용자가 인스펙터에서 검증한 Scale 확대 방식을 동일하게 적용한다.
- 대상 패널의 X/Y Scale을 `1.01`로 설정해 화면 경계 바깥까지 덮는다.
- 공용 프리팹과 씬에 직접 저장된 패널을 모두 수정한다.

## 적용 대상

- `MenuPanel.prefab/Panel`
- `Equip_panel.prefab/background`
- `Title.unity/BackPanel`
- `Lobby.unity/BackgroundPanel`
- `Battle.unity/Background` 2개
- `Art Test Scene.unity/Background`
- `DebugBattle.unity/MenuPanel/Panel`
