# WebGL Vefects 셰이더 호환화 계획

## 범위

WebGL Player 빌드를 중단한 Vefects URP 셰이더 13개만 수정한다. 데모 프리팹, 머티리얼, 씬 및 그 밖의 Vefects 셰이더는 변경하지 않는다.

## 변경

각 대상 셰이더의 HLSL include 블록에서 `#pragma target 4.5`를 `#pragma target 3.5`로 변경한다. target 3.5 초과일 때 WebGL2/GLES3 컴파일을 강제 중단하는 Amplify Shader Editor 생성 `#error` 블록은 제거한다.

## 대상

- Anime VFX URP: `SH_VFX_Stylized_Dissolve`, `SH_VFX_Fresnel_Bomb`, `SH_VFX_Stylized_Slash_01`, `SH_VFX_Flat`
- Stylized AoE URP: `SH_Vefects_URP_Windup_Core_Add_01`, `SH_Vefects_URP_Unlit_Simple_01`, `SH_Vefects_URP_Central_Area_01`, `SH_Vefects_URP_Coaster_01`, `SH_Vefects_URP_Unlit_Simple_LUT_01`, `SH_Vefects_URP_Dust_Puffs_01`, `SH_Vefects_URP_Splashes_01`, `SH_Vefects_URP_Unlit_Master_01`, `SH_Vefects_URP_Light_Rays_Add_01`

## 검증

EditMode 소스 회귀 테스트로 대상 셰이더의 target 3.5와 WebGL 차단 `#error` 제거를 확인한다. 이후 Unity WebGL Build & Run으로 실제 셰이더 컴파일 결과를 확인한다.

