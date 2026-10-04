# SampleScene 화살표 Soft Glow 설계

## 목표

- `SampleScene.unity`의 `Arrow/Left`, `Arrow/Right`가 선명한 외곽선이 아니라 흰 코어 주변으로 빛이 부드럽게 번지는 형태로 보이게 한다.
- Screen Space Overlay Canvas에서도 동작하고 씬 전체 포스트 프로세싱에는 영향을 주지 않는다.

## 조사 결과

- 기존 Unity UI `Outline`은 원본 버텍스를 여러 방향으로 복제하므로 경계가 계단식으로 선명하게 남는다.
- SampleScene의 Canvas는 Screen Space Overlay이며 카메라 포스트 프로세싱도 꺼져 있어 Bloom 의존 방식은 적합하지 않다.
- 화살표 원본은 `45x85` 스프라이트이며 Left는 같은 이미지를 X축 반전해 사용한다.

## 설계

- `Left`, `Right`는 선명한 흰색 코어 이미지로 유지하고 기존 `Outline`을 제거한다.
- 같은 `Arrow` 루트 아래에서 코어보다 먼저 렌더링되는 `Glow_Left`, `Glow_Right` 이미지를 추가한다.
- 두 Glow 이미지는 동일한 스프라이트와 `ArrowSoftGlow` 머티리얼을 공유한다.
- 전용 UI 셰이더가 주변 알파를 다중 샘플링해 부드러운 광원을 만들며 Additive Blend로 배경에 빛을 더한다.
- Glow RectTransform은 코어보다 상하좌우 14px씩 넓혀 번짐이 잘리지 않게 한다.
- 광원 색상, 알파, 반경, 부드러움, 강도는 머티리얼에서 조정 가능하게 둔다.
- Glow 이미지는 입력을 막지 않도록 Raycast Target을 끈다.

## 기본값

- Glow Color: 흰색, Alpha `0.7`
- Radius: `6`
- Softness: `1.6`
- Strength: `2`
- Padding: 상하좌우 `14px`

## 검증

- Left/Right에 기존 Outline이 남지 않았는지 확인한다.
- 두 Glow 이미지가 동일한 머티리얼과 스프라이트를 사용하며 올바른 위치와 패딩을 갖는지 EditMode 계약 테스트로 확인한다.
- 셰이더와 테스트 어셈블리의 컴파일을 확인한다.
- Unity 에디터가 열려 있다고 가정하며 batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

고정 UI 렌더링 계층만 변경하므로 전투 상태, 랜덤, 네트워크 동기화에는 영향이 없다.
