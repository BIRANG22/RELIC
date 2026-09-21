# Discord Social SDK Plugin Build Failure Fix

## 조사 결과

Unity 빌드는 `discord_krisp` 또는 `discord_partner_sdk` 플러그인이 중복 활성화되어 실패한다. `x86_64/Debug`와 `x86_64/Release` 경로의 같은 이름 DLL이 모두 Editor, Win, Win64 대상에 활성화되면 Unity가 동일 DLL을 두 번 임포트한다.

## 권장 설계

프로젝트의 현재 Debug SDK 구성을 유지하고, Release 경로 DLL은 Windows 대상에서 비활성화한다. 이 수정에서는 `Release/discord_partner_sdk.dll.meta`의 Editor, Win, Win64 플랫폼을 비활성화한다. 다른 SDK 파일과 게임 코드, 씬, 프리팹은 수정하지 않는다.

## 검증 계획

1. 수정 전 Debug/Release `discord_krisp.dll`의 플랫폼 활성화 중복을 확인한다.
2. Debug 메타 파일의 중복 플랫폼만 비활성화한다.
3. 메타 파일을 재검사하여 Release만 활성화되었음을 확인한다.
4. Unity 에디터에서 재임포트 후 Player 빌드로 실제 빌드 성공 여부를 확인한다.

## 테스트 적용 범위

이번 수정은 Unity 네이티브 플러그인 임포터 메타데이터 설정만 변경하며 런타임 로직을 추가하거나 변경하지 않는다. 따라서 새 자동화 테스트는 만들지 않고, Unity 재임포트 및 Player 빌드를 회귀 검증으로 사용한다.
