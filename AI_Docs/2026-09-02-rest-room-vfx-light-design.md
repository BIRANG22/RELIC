# 휴식방 모닥불 VFX 및 2D 조명 설계

## 문제 원인

- `Share_Restroom/Vfx_bonfire`는 VFX Graph(`VisualEffect`)이며, 전투 메인 카메라는 URP 2D Renderer를 사용한다. 2D Renderer는 해당 VFX Graph 출력을 직접 렌더하지 않는다.
- `Freeform Light 2D`는 Sprite-Lit 대상 정렬 레이어를 포함하지만 Blend Style 0(Multiply)을 사용한다. 어두운 붉은 색이 곱해져 발광이 아니라 어둡게 보인다.
- URP 2D Light는 Sprite-Lit 렌더러에만 적용되며 VFX Graph 자체에는 적용되지 않는다.

## 설계

1. `Vfx_bonfire`에 `RestRoomVfxOverlayCamera` 컴포넌트를 둔다.
   - 활성화 중 메인 카메라를 추적하는 보조 카메라를 생성한다.
   - 보조 카메라는 Forward Renderer(인덱스 1), `VFX` 레이어만 사용하고, Depth만 지운 뒤 메인 카메라 다음에 렌더한다.
   - 비활성화 시 보조 카메라를 제거한다. 따라서 `Share_Restroom`이 활성일 때에만 동작한다.
2. `Freeform Light 2D`의 Blend Style을 Additive(인덱스 1)로 전환한다.
   - 기존 대상 정렬 레이어와 Sprite-Lit 머티리얼은 유지한다.

## 범위 및 영향

- `Share_Restroom`의 모닥불 VFX와 조명 표현만 변경한다.
- 전투 결과, 맵 데이터, UI 및 멀티플레이 상태에는 영향을 주지 않는다.
