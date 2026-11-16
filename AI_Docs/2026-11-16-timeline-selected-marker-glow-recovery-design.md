# 타임라인 선택 마커 글로우 복구 설계

## 조사 결과

- 선택된 타임라인 마커의 흰색 회전 효과는 `SelectedButtonBorderGlow`와 `TimelineSlot.prefab`의 `Glowimage`가 담당한다.
- 프리팹의 머티리얼 참조 GUID는 남아 있지만, 커밋 `2b34642f2`에서 `Glow_Material.mat`과 `TurnMark_BorderFlow.shader` 및 메타 파일이 삭제됐다.
- 효과를 최초 추가한 커밋은 `26793bc75`다.

## 복구 설계

- `2b34642f2` 직전 버전의 `Assets/Project/Art/YDJ` 폴더 메타, 머티리얼, 셰이더 및 각 메타 파일을 원본 그대로 복구한다.
- 기존 프리팹과 `SelectedButtonBorderGlow` 코드는 수정하지 않는다.
- GUID와 셰이더 연결이 유지되는지 EditMode 테스트로 보호한다.

## 멀티플레이 영향

- 없음. UI 셰이더와 머티리얼 리소스만 복구하며 전투 상태 및 네트워크 데이터는 변경하지 않는다.
