# Localization Manager 에디터 오류 수정 설계

## 조사 결과

- `전체 검사 및 적용`의 씬 검사가 `OpenPreviewScene`을 사용해 현재 열린 씬과 검사 대상 씬의 `Global Light 2D`를 동시에 등록하고 있었다.
- 프리팹·씬의 TMP 텍스트를 검사하는 동안 Dynamic TMP 폰트가 글리프와 아틀라스를 갱신할 수 있었다.
- 바인딩 복구가 마지막에 전역 `AssetDatabase.SaveAssets()`를 호출하여 검사와 무관한 동적 폰트의 임시 아틀라스 상태까지 저장 대상으로 포함했다.

## 권장 설계

1. 씬 검사는 Preview Scene 대신 `OpenSceneMode.Single`로 한 장씩 열고, 작업 전 Scene Setup을 작업 종료 시 반드시 복원한다.
2. Localization Manager 작업 시작 전에 수정 중인 씬의 저장 여부를 확인한다.
3. 작업 범위 동안 프로젝트의 Dynamic TMP 폰트를 임시로 Static 모드로 보호하고, 성공·예외 여부와 무관하게 원래 모드와 dirty 상태를 복원한다.
4. 바인딩 복구와 정적 마이그레이션은 변경된 프리팹·씬만 명시적으로 저장하고 전역 `SaveAssets()`를 호출하지 않는다.
5. Excel Import는 전체 프로젝트가 아니라 갱신한 String Table Collection, Shared Data, String Table만 `SaveAssetIfDirty`로 저장한다.

## 검증 계획

- TMP 폰트 보호 범위 안에서 Dynamic 폰트가 Static으로 전환되는지 확인한다.
- 정상 종료와 예외 종료 모두에서 원래 폰트 모드가 복원되는지 확인한다.
- Editor 어셈블리 컴파일로 API와 테스트 컴파일을 확인한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
