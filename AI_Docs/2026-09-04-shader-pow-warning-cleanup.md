# Shader pow 경고 정리

## 대상

- `Master_Unlit.shadergraph`: VFX Graph `VFX_Slash_13`, `VFX_Slash_20`의 공통 원본 그래프
- `MasterSlash_Unlit.shadergraph`: 별도 Slash Shader Graph
- `transition.shadergraph`: 화면 전환 Shader Graph

## 변경

각 `Power` 노드의 Base 입력 직전에 `Maximum(Base, 0)` 노드를 추가했다.
상한을 1로 제한하는 Saturate는 사용하지 않아 기존의 1 초과 밝기와 강도는 유지한다.
`transition`도 Progress 입력 자체가 아니라 Power에 전달되는 덧셈 결과만 보정한다.

## 제외

`Hidden/Light 2D`의 vector truncation 경고는 Unity 6000.0.68f1 URP 패키지 캐시의
`Light2D.shader`에서 발생한다. 프로젝트 소스로 덮어쓰거나 PackageCache를 직접 수정하지 않는다.
Unity 및 URP를 함께 업데이트한 뒤 재확인한다.

## 검증

- 원본 그래프별 보정 노드 수를 검사하는 EditMode 테스트 추가
- Assembly-CSharp 및 Assembly-CSharp-Editor 컴파일 성공
- Unity Editor 재임포트 및 실제 VFX 재생 확인은 Editor에서 별도 수행 필요
