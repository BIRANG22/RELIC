# 로비 연구 보상 VFX 선행 표시

## 목적

배틀 종료 후 로비로 돌아왔을 때 보상 정산 패널을 열기 전에, 로비에 배치된 `Vfx_BlueDustium`을 한 번 재생한다.

## 흐름

1. `ResearchResultPanelUI`가 기존처럼 보상 정산 및 HUD 갱신을 수행한다.
2. 패널을 열지 않고 `ResearchResultPanelUI`가 로비에 배치된 VFX를 재생한다.
3. 패널은 기존 `LobbyPositionModalInputBlocker`만 점유하고, VFX의 모든 파티클을 재시작한다.
4. 모든 파티클이 종료되면 차단을 해제하고 패널을 표시한다.

## 구성 원칙

- VFX는 새로 인스턴스화하지 않고 로비 씬에 배치된 오브젝트를 사용한다.
- VFX 및 입력 차단은 보상 결과를 계산하거나 변경하지 않는다.
- 씬 참조는 `ResearchResultPanelUI`의 serialized reference로 연결한다.
- 입력 누락이 확인될 때만 기존 차단기 검사 범위를 추가한다.
