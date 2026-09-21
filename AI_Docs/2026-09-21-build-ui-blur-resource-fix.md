# 빌드 UI 배경 블러 리소스 복구 설계

## 조사 결과

- `UIBlurBackgroundManager`는 `Resources.Load<Material>("UI/DustiumBackgroundBlur")`를 우선 사용한다.
- 현재 `DustiumBackgroundBlur.mat`은 `Assets/Project/Art/Materials`에 있어 Resources 로드 대상이 아니다.
- 에디터에서는 `Shader.Find("UI/DustiumBackgroundBlur")` 대체 경로가 동작할 수 있지만, 빌드에서는 참조가 끊긴 셰이더가 스트리핑될 수 있다.
- 과거 `buildblur 정상화` 변경에서도 동일 머티리얼을 `Assets/Project/Resources/UI`에 두어 빌드 포함을 보장했다. 이후 아트 리소스 정리 과정에서 해당 경로가 변경됐다.
- PC 렌더러에는 `UIBackgroundBlurRendererFeature`가 활성화되어 있으므로 렌더러 기능 누락은 현재 원인이 아니다.

## 권장 설계

기존 머티리얼과 메타 파일을 `Assets/Project/Resources/UI`로 되돌려 GUID와 셰이더 연결을 유지한다. 런타임 코드, 패널, 씬, 프리팹은 변경하지 않는다.

## 구현 계획

1. Resources 경로에서 블러 머티리얼을 실제로 로드하는 EditMode 회귀 테스트를 먼저 추가한다.
2. 테스트가 머티리얼 누락으로 실패하는 상태를 확인한다.
3. 기존 머티리얼과 메타 파일을 Resources 경로로 이동한다.
4. 셰이더 이름·지원 여부·필수 프로퍼티를 테스트한다.
5. 스크립트 컴파일, 에셋 경로/GUID, 렌더러 기능 설정을 검증한다.

## 멀티플레이 경계

UI 렌더링 리소스의 빌드 포함만 복구한다. 전투 상태, 명령, 결과 이벤트, 랜덤 및 네트워크 경계에는 영향을 주지 않는다.
